using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace WorkshopTool
{
	public partial class AddProjectToListWindow : Window
	{
		public string WorkshopItemTitle;
		public string ProjectPath;
		
		public AddProjectToListWindow()
		{
			InitializeComponent();
			
			Loaded += (sender, args) => {
				if (ProjectPath != null) {
					TbProjectPath.Text = ProjectPath;
				}

				if (WorkshopItemTitle != null) {
					Title = "Set Mod Project to Workshop Item";
					TbMessage.Text = $"Select the path of a mod project you want to set to the workshop item '{WorkshopItemTitle}'";
					BtOk.Content = "Set";
				} else {
					Title = "Add Mod Project to List";
					TbMessage.Text = $"Select the path of a mod project you want to add to the list";
					BtOk.Content = "Add";
				}
			};
		}

		private void Button_OnClickAssociate(object sender, RoutedEventArgs e)
		{
			string projectPath = TbProjectPath.Text.Trim();
			
			bool foundProject = 
				Directory.Exists(projectPath) &&
				Directory.EnumerateFiles(projectPath, "*.*", SearchOption.AllDirectories)
					.Any(fp => Path.GetFileName(fp).ToLowerInvariant() == App.ModMetaJsonPath);

			if (!foundProject) {
				MessageBox.Show(
					this,
					"Selected directory does not contain mod project path.",
					"Wrong Directory",
					MessageBoxButton.OK,
					MessageBoxImage.Error);

				return;
			}

			ProjectPath = projectPath;
			DialogResult = true;
			Close();
		}

		private void Button_OnClickCancel(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
		
		private void Button_OnClickProjectPath(object sender, RoutedEventArgs e)
		{
			var dialog = new FolderBrowserDialog {
				Description = "Select the directory in which the mod project is located.",
				SelectedPath = TbProjectPath.Text,
			};

			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				TbProjectPath.Text = dialog.SelectedPath;
			}
		}
	}
}