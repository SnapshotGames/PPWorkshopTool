using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace WorkshopTool
{
	public partial class NewProjectWindow : Window
	{
		public WorkshopItem WorkshopItem;
		
		public NewProjectWindow()
		{
			InitializeComponent();
			
			Loaded += (sender, args) => {
				string name = WorkshopItem != null ? WorkshopItem.Title : "New Mod";
				TbId.Text = $"com.example.{App.GetShortName(name).ToLowerInvariant()}";
				TbName.Text = name;
				TbAuthor.Text = App.LocalAppData.GetDefaultAuthor();
				TbDescription.Text = $"{name} description";
				TbProjectPath.Text = App.LocalAppData.GetDefaultProjectPath();
				TbId.Focus();
				TbId.CaretIndex = TbId.Text.Length;
			};
		}
		
		public async Task<WorkshopItem> RunAsyncOperation()
		{ 
			return await App.CreateModProject(
				TbId.Text.Trim(),
				TbName.Text.Trim(),
				TbAuthor.Text.Trim(),
				TbDescription.Text.Trim(),
				TbProjectPath.Text.Trim());
		}
		
		private void Button_OnClickOk(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TbId.Text)) {
				MessageBox.Show(
					this,
					"ID cannot be empty",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);

				TbId.Focus();
				return;
			}

			if (string.IsNullOrWhiteSpace(TbName.Text)) {
				MessageBox.Show(
					this,
					"Name cannot be empty",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);

				TbName.Focus();
				return;
			}
			
			if (string.IsNullOrWhiteSpace(TbAuthor.Text)) {
				MessageBox.Show(
					this,
					"Author cannot be empty",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);

				TbAuthor.Focus();
				return;
			}
			
			if (string.IsNullOrWhiteSpace(TbDescription.Text)) {
				MessageBox.Show(
					this,
					"Description cannot be empty",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);

				TbDescription.Focus();
				return;
			}
			
			if (string.IsNullOrWhiteSpace(TbProjectPath.Text)) {
				MessageBox.Show(
					this,
					"Project path cannot be empty",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);

				TbProjectPath.Focus();
				return;
			}
			
			if (!Directory.Exists(TbProjectPath.Text)) {
				MessageBox.Show(
					this,
					"Project path does not exist",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);

				TbProjectPath.Focus();
				return;
			}

			App.LocalAppData.DefaultAuthor = TbAuthor.Text.Trim();
			App.LocalAppData.DefaultProjectPath = TbProjectPath.Text.Trim();
			App.WriteLocalData();
			DialogResult = true;
			Close();
		}


		private void Button_OnClickCancel(object sender, RoutedEventArgs e)
		{
			Close();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			if (DialogResult == true) {
				return;
			}
			
			MessageBoxResult result = MessageBox.Show(
				this,
				"Cancel the creation of new mod project?",
				"Cancel",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
			
			if (result == MessageBoxResult.Yes) {
				DialogResult = false;
			} else {
				e.Cancel = true;
			}
		}

		private void Button_OnClickProjectPath(object sender, RoutedEventArgs e)
		{
			var dialog = new FolderBrowserDialog {
				Description = "Select directory to store the project. The project is going to be created in its own subdirectory within that directory.",
				SelectedPath = TbProjectPath.Text,
			};

			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				TbProjectPath.Text = dialog.SelectedPath;
			}
		}
	}
}