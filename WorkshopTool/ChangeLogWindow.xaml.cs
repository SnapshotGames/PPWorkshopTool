using System.ComponentModel;
using System.Windows;

namespace WorkshopTool
{
	public partial class ChangeLogWindow : Window
	{
		public ChangeLogWindow()
		{
			InitializeComponent();
		}

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			if (DialogResult == true) {
				return;
			}

			DialogResult = false;
		}
	}
}