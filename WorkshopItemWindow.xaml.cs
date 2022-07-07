using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Steamworks.Ugc;

namespace WorkshopTool
{
	public partial class WorkshopItemWindow : Window
	{
		public WorkshopItem WorkshopItem;

		private Editor _asyncOperation;
		
		public WorkshopItemWindow()
		{
			InitializeComponent();
			
			Loaded += (sender, args) => {
				Title = "Create New Workshop Item";
				string name = WorkshopItem != null ? WorkshopItem.Title : "New Item";
				TbTitle.Text = name;
				TbDescription.Text = $"{name} Description";
				TbTags.Text = "Tag1, Tag2";
				TbTitle.Focus();
			};
		}
		
		public async Task<PublishResult> RunAsyncOperation(IProgress<float> progress)
		{
			PublishResult result = await _asyncOperation.SubmitAsync(progress);
			return result;
		}
		
		private void Button_OnClickOk(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TbTitle.Text)) {
				MessageBox.Show(
					this,
					"Title cannot be empty",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);

				TbTitle.Focus();
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

			if (!string.IsNullOrWhiteSpace(TbThumbnailPath.Text) &&
			    !File.Exists(TbThumbnailPath.Text)) 
			{
				MessageBox.Show(
					this,
					"Thumbnail image file does not exist",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);

				TbThumbnailPath.Focus();
				return;
			}
			
			var logWindow = new ChangeLogWindow {
				Owner = this,
			};
			
			bool result = logWindow.ShowDialog() ?? false;
			
			if (!result) {
				return;
			}

			string logMessage = logWindow.TbLogMessage.Text;

			// New item
			_asyncOperation = Editor.NewCommunityFile
				.ForAppId(App.PhoenixPointAppSteamId)
				.WithTitle(TbTitle.Text.Trim())
				.WithDescription(TbDescription.Text.Trim())
				.WithChangeLog(logMessage.Trim());

			if (!string.IsNullOrWhiteSpace(TbTags.Text)) {
				IEnumerable<string> tags = TbTags.Text
					.Trim()
					.Split(',')
					.Select(tag => tag.Trim());
				
				foreach (string tag in tags) {
					_asyncOperation.WithTag(tag);
				}
			}

			string thumbnailPath = TbThumbnailPath.Text;
			if (!string.IsNullOrWhiteSpace(thumbnailPath)) {
				thumbnailPath = thumbnailPath.Trim().Replace('\\', '/');
				_asyncOperation.WithPreviewFile(thumbnailPath);
			}
			
			WorkshopItemVisibility visibility = 
				(WorkshopItemVisibility) CbVisibility.SelectedIndex;

			if (visibility == WorkshopItemVisibility.Public) {
				_asyncOperation.WithPublicVisibility();
			} else if (visibility == WorkshopItemVisibility.FriendsOnly) {
				_asyncOperation.WithFriendsOnlyVisibility();
			} else {
				_asyncOperation.WithPrivateVisibility();
			}
			
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
				"Cancel the creation of new Workshop Item?",
				"Cancel",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
			
			if (result == MessageBoxResult.Yes) {
				DialogResult = false;
			} else {
				e.Cancel = true;
			}
		}

		private void Button_OnClickThumbnailPath(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog {
				Title = "Select Image",
				Filter = "Images|*.png;*.jpg;*.gif" +
				         "|All Files|*.*",
				CheckFileExists = true
			};

			bool? result = dialog.ShowDialog();

			if (result == true) {
				TbThumbnailPath.Text = dialog.FileName;
			}
		}
	}
}
