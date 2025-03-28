using System.Windows;

namespace Numbered_Folder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string[] args = e.Args;

            if (args.Length == 0)
            {
                new MainWindow().Show();
            }
            else if (args.Length == 1)
            {
                new FolderWindow(args[0]).Show();
            }
        }
    }
}
