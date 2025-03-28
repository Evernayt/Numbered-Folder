using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Numbered_Folder
{
    public partial class MainWindow : Window
    {
        private static readonly Regex invalidFolderNameChars = new(@"[\\/:*?""<>|]", RegexOptions.Compiled);

        private readonly string? _appPath = Environment.ProcessPath;

        private const string SUB_MENU_KEY = $@"SOFTWARE\Classes\Directory\shell\NumberedFolder";
        private const string BG_SUB_MENU_KEY = $@"SOFTWARE\Classes\Directory\Background\shell\NumberedFolder";
        private const string ACTION_TEXT = "Создать папку";

        private DispatcherTimer _timer = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadFolderSuggestions();

            if (IsContextMenuCreated())
            {
                ShowDeleteButton();
            }

            _timer = new()
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _timer.Tick += Timer_Tick;
        }

        private static bool IsContextMenuCreated()
        {
            using RegistryKey? subMenu = Registry.CurrentUser.OpenSubKey(SUB_MENU_KEY);
            using RegistryKey? bgSubMenu = Registry.CurrentUser.OpenSubKey(BG_SUB_MENU_KEY);
            return subMenu != null && bgSubMenu != null;
        }

        private void CreateContextMenu()
        {
            if (_appPath == null) return;

            using RegistryKey subMenu = Registry.CurrentUser.CreateSubKey(SUB_MENU_KEY);
            subMenu.SetValue("MUIVerb", ACTION_TEXT);
            subMenu.SetValue("Position", "Top");
            subMenu.SetValue("Icon", _appPath);

            string subMenuCommandKey = SUB_MENU_KEY + @"\command";
            using RegistryKey subMenuCommand = Registry.CurrentUser.CreateSubKey(subMenuCommandKey);
            subMenuCommand.SetValue("", $"\"{_appPath}\" \"%V\"");


            using RegistryKey bgSubMenu = Registry.CurrentUser.CreateSubKey(BG_SUB_MENU_KEY);
            bgSubMenu.SetValue("MUIVerb", ACTION_TEXT);
            bgSubMenu.SetValue("Position", "Top");
            bgSubMenu.SetValue("Icon", _appPath);

            string bgSubMenuCommandKey = BG_SUB_MENU_KEY + @"\command";
            using RegistryKey bgSubMenuCommand = Registry.CurrentUser.CreateSubKey(bgSubMenuCommandKey);
            bgSubMenuCommand.SetValue("", $"\"{_appPath}\" \"%V\"");
        }

        private static void DeleteContextMenu()
        {
            Registry.CurrentUser.DeleteSubKeyTree(SUB_MENU_KEY, false);
            Registry.CurrentUser.DeleteSubKeyTree(BG_SUB_MENU_KEY, false);
        }

        private void ShowDeleteButton()
        {
            AddBtn.Visibility = Visibility.Collapsed;
            DeleteBtn.Visibility = Visibility.Visible;
            WarningImg.Visibility = Visibility.Collapsed;
            SuccessImg.Visibility = Visibility.Visible;
            InfoTbk.Text = "Контекстное меню создано";
        }

        private void ShowAddButton()
        {
            AddBtn.Visibility = Visibility.Visible;
            DeleteBtn.Visibility = Visibility.Collapsed;
            WarningImg.Visibility = Visibility.Visible;
            SuccessImg.Visibility = Visibility.Collapsed;
            InfoTbk.Text = "Контекстное меню не создано";
        }

        private void LoadFolderSuggestions()
        {
            if (_appPath == null) return;

            string? appFolder = Path.GetDirectoryName(_appPath);
            if (appFolder == null) return;

            string filePath = Path.Combine(appFolder, "folder-suggestion.ini");

            if (File.Exists(filePath))
            {
                string folderSuggestionsText = File.ReadAllText(filePath);
                FolderSuggestionTbx.Text = folderSuggestionsText;
            }
        }

        private void SaveFolderSuggestions()
        {
            if (_appPath == null) return;

            string? appFolder = Path.GetDirectoryName(_appPath);
            if (appFolder == null) return;

            string filePath = Path.Combine(appFolder, "folder-suggestion.ini");
            File.WriteAllText(filePath, FolderSuggestionTbx.Text);

            SavedTbk.Visibility = Visibility.Visible;
            _timer.Start();
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            CreateContextMenu();
            ShowDeleteButton();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            DeleteContextMenu();
            ShowAddButton();
        }

        private void SuggestionsBtn_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Collapsed;
            SuggestionsGrid.Visibility = Visibility.Visible;
        }
        
        private void BacksBtn_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Visible;
            SuggestionsGrid.Visibility = Visibility.Collapsed;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFolderSuggestions();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();
            SavedTbk.Visibility = Visibility.Collapsed;
        }

        private void FolderSuggestionTbx_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (invalidFolderNameChars.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        private void FolderSuggestionTbx_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pastedText = (string)e.DataObject.GetData(typeof(string));
                string cleanedText = invalidFolderNameChars.Replace(pastedText, string.Empty);

                if (cleanedText != pastedText)
                {
                    int caretIndex = FolderSuggestionTbx.CaretIndex;
                    FolderSuggestionTbx.Text = FolderSuggestionTbx.Text.Insert(caretIndex, cleanedText);
                    FolderSuggestionTbx.CaretIndex = caretIndex + cleanedText.Length;

                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}