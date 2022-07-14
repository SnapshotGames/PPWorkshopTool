using System.Windows;

namespace WorkshopTool
{
	public partial class AboutWindow : Window
	{
		public AboutWindow()
		{
			InitializeComponent();
		}

		private void Button_OnClickOk(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
