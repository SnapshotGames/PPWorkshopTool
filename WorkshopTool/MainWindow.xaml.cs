using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using log4net;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;

namespace WorkshopTool
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, IProgress<float>
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(MainWindow));

		private class ListViewItem
		{
			// WPF data binding 
			public string LvFileId => WorkshopItem.FileId == 0 ? "<local>" : WorkshopItem.FileId.ToString();
			public string LvTitle => WorkshopItem.Title;
			public string LvProjectPath => string.IsNullOrWhiteSpace(WorkshopItem.ProjectPath) ? "<no project>" : WorkshopItem.ProjectPath;

			public WorkshopItem WorkshopItem;
		}
		
		public MainWindow()
		{
			if (!SteamClient.IsValid) {
				return;
			}

			InitializeComponent();
			Dispatcher.BeginInvoke(new Func<Task>(RefreshWorkshopItems));
		}

		private async Task RefreshWorkshopItems()
		{
			StartAsyncOperation("Loading workshop items...");

			Query q = Query.All
				.WhereUserPublished()
				.ForAppId(App.PhoenixPointAppSteamId)
				.WithCreatorAppId(App.WorkshopToolSteamId);

			int page = 1;
			var existingFileIds = new HashSet<PublishedFileId>();
			
			while (true) {
				ResultPage? items = await q.GetPageAsync(page);

				if (items == null || items.Value.ResultCount == 0) {
					break;
				}
	
				foreach (Item entry in items.Value.Entries) {
					existingFileIds.Add(entry.Id);
					WorkshopItem item = App.LocalAppData.GetById(entry.Id);
					
					if (item == null) {
						item = new WorkshopItem {
							FileId = entry.Id,
						};
						
						App.LocalAppData.Add(item);
					}

					item.Title = entry.Title;
					item.Description = entry.Description;
					item.Tags = string.Join(", ", entry.Tags);
					
					if (entry.IsPublic) {
						item.Visibility = WorkshopItemVisibility.Public;
					} else if (entry.IsFriendsOnly) {
						item.Visibility = WorkshopItemVisibility.FriendsOnly;
					} else {
						item.Visibility = WorkshopItemVisibility.Private;
					}
				}

				++ page;
			}

			for (int i = 0; i < App.LocalAppData.WorkshopItems.Count;) {
				WorkshopItem item = App.LocalAppData.WorkshopItems[i];

				if (item.FileId != default && !existingFileIds.Contains(item.FileId)) {
					// Item got deleted on Steam. Remove it from local storage if it doesn't have project associated
					if (item.ProjectPath == null) {
						App.LocalAppData.WorkshopItems.RemoveAt(i);
						continue;
					}

					item.FileId = default;
				}

				++ i;
			}
			
			App.WriteLocalData();
			LvModsList.Items.Clear();
			
			foreach (WorkshopItem item in App.LocalAppData.WorkshopItems.OrderBy(wi => wi.Title)) {
				LvModsList.Items.Add(new ListViewItem {
					WorkshopItem = item,
				});
			}
			
			StopAsyncOperation();
		}
		
		private async void Menu_OnClickNewModProject(object sender, RoutedEventArgs e)
		{
			var newProjectDialog = new NewProjectWindow {
				Owner = this
			};
		
			WorkshopItem selectedItem = ((ListViewItem) LvModsList.SelectedItem)?.WorkshopItem;
			
			bool workshopItemWithoutProjectSelected =
				selectedItem != null && selectedItem.FileId != 0 && selectedItem.ProjectPath == null;
			
			if (workshopItemWithoutProjectSelected) {
				newProjectDialog.WorkshopItem = selectedItem;
			}
			
			bool confirmed = newProjectDialog.ShowDialog() ?? false;

			if (!confirmed) {
				return;
			}

			WorkshopItem newItem;
			
			try {
				StartAsyncOperation("Creating new mod project...");
				newItem = await newProjectDialog.RunAsyncOperation();
			} catch (Exception ex) {
				Log.Error("Error creating mod project", ex);
				
				MessageBox.Show(
					this,
					$"Error creating Workshop Item: {ex.Message}",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
				
				return;
			} finally {
				StopAsyncOperation();
			}

			bool workshopItemAssociatedWithProject = false;
			
			if (workshopItemWithoutProjectSelected) {
				MessageBoxResult result = MessageBox.Show(
					this,
					$"Do you want to associate the new project with Workshop item '{selectedItem.Title}'?",
					"Associate project with Workshop Item",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question);

				if (result == MessageBoxResult.Yes) {
					selectedItem.ProjectPath = newItem.ProjectPath;
					App.WriteLocalData();
					workshopItemAssociatedWithProject = true;
				}
			}

			if (!workshopItemAssociatedWithProject) {
				App.LocalAppData.WorkshopItems.Add(newItem);
				App.WriteLocalData();
			}

			await RefreshWorkshopItems();
			
			if (workshopItemWithoutProjectSelected) {
				SelectItemWithId(selectedItem.FileId);
			} else {
				SelectItemWithProjectPath(newItem.ProjectPath);
			}
		}
		
		private async void MenuItem_OnClickRemoveModProject(object sender, RoutedEventArgs e)
		{
			WorkshopItem workshopItem = ((ListViewItem) LvModsList.SelectedItem)?.WorkshopItem;
			
			if (workshopItem == null) {
				return;
			}
			
			if (workshopItem.ProjectPath == null) {
				MessageBox.Show(
					this,
					$"The Workshop item '{workshopItem.Title}' does not have a project associated with it.",
					"Missing project",
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation);

				return;
			}
			
			string question = workshopItem.FileId == 0 ? 
				$"Remove mod project '{workshopItem.ProjectPath}' from the list? This will not delete the project from your computer." :
				$"Remove mod project '{workshopItem.ProjectPath}' from workshop item '{workshopItem.Title}'? This will not delete the project from your computer.";
			
			MessageBoxResult result = MessageBox.Show(
				this,
				question,
				"Missing project",
				MessageBoxButton.YesNo,
				MessageBoxImage.Exclamation);

			if (result == MessageBoxResult.No) {
				return;
			}

			if (workshopItem.FileId == 0) {
				App.LocalAppData.WorkshopItems.Remove(workshopItem);
				App.WriteLocalData();
			} else {
				workshopItem.ProjectPath = null;
			}

			await RefreshWorkshopItems();

			if (workshopItem.FileId != 0) {
				SelectItemWithId(workshopItem.FileId);
			}
		}
		
		private async void MenuItem_OnClickAddModProjectToList(object sender, RoutedEventArgs e)
		{
			var dialog = new AddProjectToListWindow();
			bool result = dialog.ShowDialog() ?? false;

			if (!result) {
				return;
			}

			// Check to see if we already have this project in the list
			string normProjectPath = App.NormalizePath(dialog.ProjectPath);
			
			foreach (ListViewItem lvItem in LvModsList.Items) {
				WorkshopItem workshopItem = lvItem.WorkshopItem;
				
				if (workshopItem.FileId == 0 &&
				    normProjectPath == App.NormalizePath(workshopItem.ProjectPath)) 
				{
					MessageBox.Show(
						this,
						"The project is already in the list.",
						"Existing Project",
						MessageBoxButton.OK,
						MessageBoxImage.Exclamation);

					LvModsList.Focus();
					LvModsList.SelectedItem = lvItem;
					LvModsList.ScrollIntoView(lvItem);
					return;
				}
			}
			
			App.LocalAppData.WorkshopItems.Add(new WorkshopItem {
				Title = App.GetProjectTitle(dialog.ProjectPath),
				ProjectPath = dialog.ProjectPath,
			});
			
			App.WriteLocalData();
			await RefreshWorkshopItems();
			SelectItemWithProjectPath(dialog.ProjectPath);
		}

		private async void MenuItem_OnClickSetModProjectToWorkshopItem(object sender, RoutedEventArgs e)
		{
			WorkshopItem workshopItem = ((ListViewItem) LvModsList.SelectedItem)?.WorkshopItem;

			if (workshopItem == null || workshopItem.FileId == 0) {
				MessageBox.Show(
					this,
					"You must select a Workshop Item from the list (ID column contains a number)",
					"Missing Workshop Item",
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation);
				
				return;
			}
			
			var dialog = new AddProjectToListWindow {
				WorkshopItemTitle = workshopItem.Title,
				ProjectPath = workshopItem.ProjectPath,
			};

			bool result = dialog.ShowDialog() ?? false;

			if (!result) {
				return;
			}

			workshopItem.ProjectPath = dialog.ProjectPath;
			App.WriteLocalData();
			await RefreshWorkshopItems();
			SelectItemWithId(workshopItem.FileId);
		}
		
		private void Menu_OnClickOpenModProject(object sender, RoutedEventArgs e)
		{
			WorkshopItem workshopItem = ((ListViewItem) LvModsList.SelectedItem)?.WorkshopItem;
			
			if (workshopItem == null) {
				MessageBox.Show(
					this,
					"You must select an item with an associated mod project",
					"Missing project",
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation);
				
				return;
			}

			if (workshopItem.ProjectPath == null) {
				MessageBox.Show(
					this,
					$"The Workshop item '{workshopItem.Title}' does not have a project associated with it. You can use the 'Project' menu to create a project or select an existing one and associate it with the Workshop item",
					"Missing project",
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation);

				return;
			}

			try {
				App.OpenModProject(workshopItem.ProjectPath);
			} catch (Exception ex) {
				MessageBox.Show(
					this,
					$"Error opening mod project: {ex.Message}",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}
		
		private void Menu_OnClickOpenModProjectInExplorer(object sender, RoutedEventArgs e)
		{
			WorkshopItem workshopItem = ((ListViewItem) LvModsList.SelectedItem)?.WorkshopItem;
			
			if (workshopItem == null) {
				MessageBox.Show(
					this,
					"You must select an item with an associated mod project",
					"Missing project",
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation);
				
				return;
			}

			if (workshopItem.ProjectPath == null) {
				MessageBox.Show(
					this,
					$"The Workshop item '{workshopItem.Title}' does not have a project associated with it. You can use the 'Project' menu to create a project or select an existing one and associate it with the Workshop item",
					"Missing project",
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation);

				return;
			}

			try {
				App.OpenModProjectInExplorer(workshopItem.ProjectPath);
			} catch (Exception ex) {
				MessageBox.Show(
					this,
					$"Error opening mod project in Explorer: {ex.Message}",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private void MenuItem_OnClickRestartGame(object sender, RoutedEventArgs e)
		{
			MessageBoxResult result = MessageBox.Show(
				this,
				"This will kill the current running instance of the game (if running) and will start it again. Do you want to continue?",
				"(Re)Start the Game",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (result == MessageBoxResult.No) {
				return;
			}
			
			App.RestartGame();
		}

		private void Menu_OnClickRemoveTestMod(object sender, RoutedEventArgs e)
		{
			MessageBoxResult result = MessageBox.Show(
				this,
				"Remove the mod you are currently testing from the game?",
				"Remove Test Mod",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (result == MessageBoxResult.No) {
				return;
			}

			App.RemoveTestMod();
			MessageBox.Show(
				this,
				"Test mod removed from the game. You need to restart the game for the mod to disappear from the list.",
				"Remove Test Mod",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void Menu_OnClickExit(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private async void Menu_OnClickNewWorkshopItem(object sender, RoutedEventArgs e)
		{
			WorkshopItemWindow workshopItemDialog = new WorkshopItemWindow {
				Owner = this,
			};
			
			WorkshopItem selectedItem = ((ListViewItem) LvModsList.SelectedItem)?.WorkshopItem;

			bool workshopItemWithoutIdSelected =
				selectedItem != null && selectedItem.FileId == 0;
			
			if (workshopItemWithoutIdSelected) {
				workshopItemDialog.WorkshopItem = selectedItem;
			}
			
			bool confirmed = workshopItemDialog.ShowDialog() ?? false;

			if (!confirmed) {
				return;
			}
			
			StartAsyncOperation("Creating new Workshop item...");
			PublishResult result = await workshopItemDialog.RunAsyncOperation((IProgress<float>) this);

			if (!result.Success) {
				Log.Error("Error creating Workshop item: {result.Result}");
				
				MessageBox.Show(
					this,
					$"Error creating Workshop item: {result.Result}",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
			
			StopAsyncOperation();

			if (workshopItemDialog.WorkshopItem != null) {
				workshopItemDialog.WorkshopItem.FileId = result.FileId;
			}
			
			await RefreshWorkshopItems();

			if (result.Success) {
				SelectItemWithId(result.FileId);
			}
		}
		
		private async void MenuItem_OnClickRemoveWorkshopItem(object sender, RoutedEventArgs e)
		{
			WorkshopItem workshopItem = ((ListViewItem) LvModsList.SelectedItem)?.WorkshopItem;
			
			if (workshopItem == null || workshopItem.FileId == 0) {
				return;
			}

			string warning = workshopItem.ProjectPath != null
				? $"Do you want to remove '{workshopItem.Title}' from Steam Workshop? This will NOT remove the mod project from your computer.\n\nWARNING: This operation cannot be undone!"
				: $"Do you want to remove '{workshopItem.Title}' from Steam Workshop?\n\nWARNING: This operation cannot be undone!";
			
			MessageBoxResult result = MessageBox.Show(
				this,
				warning,
				"Remove Workshop Item",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (result == MessageBoxResult.No) {
				return;
			}

			bool success = await SteamUGC.DeleteFileAsync(workshopItem.FileId);
			await RefreshWorkshopItems();
			
			if (!success) {
				Log.Error($"Could not remove workshop item {workshopItem.FileId}");
				
				MessageBox.Show(
					this,
					$"Could not remove workshop item {workshopItem.FileId}",
					"Remove Workshop Item",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private async void Menu_OnClickRefreshWorkshopItems(object sender, RoutedEventArgs e)
		{
			await RefreshWorkshopItems();
		}
		
		private async void MenuItem_OnClickUploadDataToWorkshop(object sender, RoutedEventArgs e)
		{
			WorkshopItem workshopItem = ((ListViewItem) LvModsList.SelectedItem)?.WorkshopItem;
			
			if (workshopItem == null || workshopItem.FileId == 0) {
				MessageBox.Show(
					this,
					"You must select a Workshop Item that's created on Steam (use 'New Workshop Item' from the 'Workshop' menu)",
					"Wrong item selected",
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation);
				
				return;
			}

			if (workshopItem.ProjectPath == null) {
				MessageBox.Show(
					this,
					"You must select a Workshop Item that has a mod project associated with it (use 'New Mod Project' or 'Associate Project With Workshop Item' from the 'Project' menu)",
					"Wrong item selected",
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation);
				
				return;
			}
			
			MessageBoxResult mbResult = MessageBox.Show(
				this,
				$"Upload data:\n\nFrom Project: {workshopItem.ProjectPath}\nTo Workshop item: {workshopItem.Title} ({workshopItem.FileId})",
				"Upload data to Workshop",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (mbResult != MessageBoxResult.Yes) {
				return;
			}

			string directoryToUpload = App.GetProjectDistDirectory(workshopItem.ProjectPath);

			if (!Directory.Exists(directoryToUpload) || 
			    !Directory.EnumerateFileSystemEntries(directoryToUpload).Any()) 
			{
				MessageBox.Show(
					this,
					"The project is not built. Please build the project before uploading.",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
				
				return;
			}
			
			var logWindow = new ChangeLogWindow {
				Owner = this,
			};
				
			bool lwResult = logWindow.ShowDialog() ?? false;
				
			if (!lwResult) {
				return;
			}
			
			StartAsyncOperation("Uploading data to Steam...");
			
			PublishResult publishResult = await 
				new Editor(workshopItem.FileId)
				.ForAppId(App.PhoenixPointAppSteamId)
				.WithChangeLog(logWindow.TbLogMessage.Text.Trim())
				.WithContent(directoryToUpload)
				.SubmitAsync(this);
			
			StopAsyncOperation();

			if (publishResult.Success) {
				MessageBox.Show(
					this,
					$"Data uploaded successfully.",
					"Success",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			} else {
				MessageBox.Show(
					this,
					$"Failed to upload data: {publishResult.Result}",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private void Menu_OnClickOpenWorkshopItemInSteam(object sender, RoutedEventArgs e)
		{
			WorkshopItem workshopItem = ((ListViewItem) LvModsList.SelectedItem)?.WorkshopItem;
			
			if (workshopItem == null || workshopItem.FileId == 0) {
				return;
			}

			App.OpenItemPageInSteam(workshopItem.FileId);
		}
		
		private void MenuItem_OnClickHelp(object sender, RoutedEventArgs e)
		{
			App.OpenHelp();
		}
		
		private void MenuItem_OnClickReportIssues(object sender, RoutedEventArgs e)
		{
			App.OpenReportIssues();
		}
		
		private void MenuItem_OnClickAbout(object sender, RoutedEventArgs e)
		{
			new AboutWindow().ShowDialog();
		}
		
		private void CmListViewContextMenu_OnOpened(object sender, RoutedEventArgs e)
		{
			WorkshopItem workshopItem = ((ListViewItem) LvModsList.SelectedItem)?.WorkshopItem;
			bool itemSelected = workshopItem != null;
			bool workshopItemSelected = itemSelected && workshopItem.FileId != 0;
			bool modProjectSelected = itemSelected && workshopItem.ProjectPath != null;
			
			ItemCollection contextMenuItems = CmListViewContextMenu.Items;
			contextMenuItems.Clear();
			MenuItem mi;
			
			mi = new MenuItem();
			mi.Header = "_Refresh List";
			mi.Click += Menu_OnClickRefreshWorkshopItems;
			contextMenuItems.Add(mi);

			mi = new MenuItem();
			mi.Header = "_New Workshop Item...";
			mi.Click += Menu_OnClickNewWorkshopItem;
			contextMenuItems.Add(mi);

			if (workshopItemSelected) {
				mi = new MenuItem();
				mi.Header = "_Remove Workshop Item";
				mi.Click += MenuItem_OnClickRemoveWorkshopItem;
				contextMenuItems.Add(mi);
				
				mi = new MenuItem();
				mi.Header = "_Set Mod Project To Workshop Item...";
				mi.Click += MenuItem_OnClickSetModProjectToWorkshopItem;
				contextMenuItems.Add(mi);
				
				mi = new MenuItem();
				mi.Header = "_Upload Data to Workshop";
				mi.Click += MenuItem_OnClickUploadDataToWorkshop;
				contextMenuItems.Add(mi);
				
				mi = new MenuItem();
				mi.Header = "_Open Workshop Item in Steam";
				mi.Click += Menu_OnClickOpenWorkshopItemInSteam;
				contextMenuItems.Add(mi);
			}

			contextMenuItems.Add(new Separator());
			
			mi = new MenuItem();
			mi.Header = "_New Mod Project...";
			mi.Click += Menu_OnClickNewModProject;
			contextMenuItems.Add(mi);

			mi = new MenuItem();
			mi.Header = "_Add Existing Mod Project To List...";
			mi.Click += MenuItem_OnClickAddModProjectToList;
			contextMenuItems.Add(mi);

			if (modProjectSelected) {
				mi = new MenuItem();
				mi.Header = "_Remove Mod Project";
				mi.Click += MenuItem_OnClickRemoveModProject;
				contextMenuItems.Add(mi);
				
				mi = new MenuItem();
				mi.Header = "Open Mod Project in _Explorer";
				mi.Click += Menu_OnClickOpenModProjectInExplorer;
				contextMenuItems.Add(mi);
			}
			
			contextMenuItems.Add(new Separator());

			mi = new MenuItem();
			mi.Header = "(Re)Start the _Game";
			mi.Click += MenuItem_OnClickRestartGame;
			contextMenuItems.Add(mi);
			
			mi = new MenuItem();
			mi.Header = "Remove _Test Mod";
			mi.Click += Menu_OnClickRemoveTestMod;
			contextMenuItems.Add(mi);
			
			mi = new MenuItem();
			mi.Header = "_Exit";
			mi.Click += Menu_OnClickExit;
			contextMenuItems.Add(mi);
		}

		private void LvModsList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left) {
				return;
			}
			
			WorkshopItem workshopItem = ((ListViewItem) LvModsList.SelectedItem)?.WorkshopItem;

			if (workshopItem == null) {
				return;
			}

			if (workshopItem.ProjectPath != null) {
				Menu_OnClickOpenModProject(sender, e);
			} else if (workshopItem.FileId != 0) {
				Menu_OnClickOpenWorkshopItemInSteam(sender, e);
			}
		}

		private void SelectItemWithId(PublishedFileId fileId)
		{
			foreach (ListViewItem lvItem in LvModsList.Items) {
				if (lvItem.WorkshopItem.FileId == fileId) {
					LvModsList.Focus();
					LvModsList.SelectedItem = lvItem;
					LvModsList.ScrollIntoView(lvItem);
					break;
				}
			}
		}

		private void SelectItemWithProjectPath(string projectPath)
		{
			string normProjectPath = App.NormalizePath(projectPath);

			foreach (ListViewItem lvItem in LvModsList.Items) {
				if (lvItem.WorkshopItem.FileId == 0 &&
				    App.NormalizePath(lvItem.WorkshopItem.ProjectPath) == normProjectPath) 
				{
					LvModsList.Focus();
					LvModsList.SelectedItem = lvItem;
					LvModsList.ScrollIntoView(lvItem);
					break;
				}
			}
		}

		#region IProgress<int> implementation

		private string _asyncOpName;
		
		private void StartAsyncOperation(string name)
		{
			IsEnabled = false;
			_asyncOpName = name;
			LblStatus.Content = name;
		}

		private void StopAsyncOperation()
		{
			_asyncOpName = null;
			LblStatus.Content = "Ready";
			IsEnabled = true;
		}
		
		public void Report(float value)
		{
			LblStatus.Content = $"{_asyncOpName} {(int) Math.Round(value * 100)}%";
		}

		#endregion
	}
}
