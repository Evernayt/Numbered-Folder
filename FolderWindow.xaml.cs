using Numbered_Folder.Models;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Numbered_Folder
{
    public partial class FolderWindow : Window
    {
        private const string INITIAL_FOLDER_NAME = "Новая папка";

        private static readonly Regex invalidFolderNameChars = new(@"[\\/:*?""<>|]", RegexOptions.Compiled);

        private readonly string? _appPath = Environment.ProcessPath;
        private string _folderPath = "";
        private int _foldersCount = 1;
        private Dictionary<int, List<string>> _suggestionRules = [];
        private bool _isNavigatingWithArrows = false;

        public FolderWindow(string folderPath)
        {
            _folderPath = folderPath;

            InitializeComponent();

            string folderName = Path.GetFileName(folderPath);
            Title = $"Создание папки в {folderName}";

            _foldersCount = Directory.GetDirectories(folderPath).Length + 1;
            UpdateFullFolderName();

            FolderNameTbx.Focus();

            LoadFolderSuggestions(folderName);
        }

        private void UpdateFullFolderName()
        {
            string folderName = FolderNameTbx.Text;
            FullFolderNameTbk.Text = $"{_foldersCount}. {(string.IsNullOrWhiteSpace(folderName) ? INITIAL_FOLDER_NAME : folderName)}";
        }

        private async void LoadFolderSuggestions(string folderName)
        {
            if (_appPath == null) return;

            string? appFolder = Path.GetDirectoryName(_appPath);
            if (appFolder == null) return;

            string filePath = Path.Combine(appFolder, "folder-suggestion.ini");

            List<FolderSuggestion> folderSuggestions = await ParseFolderSuggestions(filePath);
            FolderSuggestion? folderSuggestion = folderSuggestions.Find(s => folderName.Contains(s.Name, StringComparison.CurrentCultureIgnoreCase));

            if (folderSuggestion != null)
            {
                _suggestionRules = folderSuggestion.Rules;
                SetFirstSuggestions();
            }
        }

        private void SetFirstSuggestions()
        {
            if (_suggestionRules.TryGetValue(1, out List<string>? suggestionRules) && suggestionRules.Count != 0)
            {
                SuggestionsLbx.ItemsSource = suggestionRules;
                ShowSuggestionsPopup(true);
            }
        }

        private static async Task<List<FolderSuggestion>> ParseFolderSuggestions(string filePath)
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(filePath)) return [];

                string[] folderSuggestionLines = File.ReadAllLines(filePath);
                List<FolderSuggestion> folderSuggestions = [];
                FolderSuggestion? folderSuggestion = null;

                foreach (string line in folderSuggestionLines)
                {
                    string trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("$"))
                    {
                        folderSuggestion = new FolderSuggestion
                        {
                            Name = trimmedLine[1..].Trim()
                        };
                        folderSuggestions.Add(folderSuggestion);
                    }
                    else if (folderSuggestion != null && !string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        string[] rules = trimmedLine.Split(',');

                        foreach (string rule in rules)
                        {
                            string[] parts = rule.Split('#');
                            if (parts.Length == 2 && int.TryParse(parts[0], out int wordIndex))
                            {
                                if (!folderSuggestion.Rules.TryGetValue(wordIndex, out List<string>? folderSuggestionRule))
                                {
                                    folderSuggestionRule = [];
                                    folderSuggestion.Rules[wordIndex] = folderSuggestionRule;
                                }
                                folderSuggestionRule.Add(parts[1]);
                            }
                        }
                    }
                }

                return folderSuggestions;
            });
        }

        private void CreateFolder()
        {
            _foldersCount = Directory.GetDirectories(_folderPath).Length + 1;
            string folderName = FolderNameTbx.Text;
            string fullFolderName = $"{_foldersCount}. {(string.IsNullOrWhiteSpace(folderName) ? INITIAL_FOLDER_NAME : folderName)}";
            Directory.CreateDirectory(Path.Combine(_folderPath, fullFolderName));
        }

        private void ShowSuggestionsPopup(bool IsOpen)
        {
            SuggestionsPopup.IsOpen = IsOpen;
            SuggestionsLbx.SelectedIndex = -1;
        }

        private void SelectSuggestion(string selectedSuggestion)
        {
            string inputText = FolderNameTbx.Text;
            string[] words = inputText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int currentWordIndex = words.Length;

            if (inputText.EndsWith(" "))
            {
                currentWordIndex++;
            }

            List<string> updatedWords = new(words);

            if (currentWordIndex > words.Length)
            {
                updatedWords.Add(selectedSuggestion);
            }
            else
            {
                if (updatedWords.Count > 0)
                {
                    updatedWords[^1] = selectedSuggestion;
                }
                else
                {
                    updatedWords.Add(selectedSuggestion);
                }
            }

            FolderNameTbx.Text = string.Join(" ", updatedWords);
            FolderNameTbx.CaretIndex = FolderNameTbx.Text.Length;
            UpdateFullFolderName();

            if (SuggestionsLbx.Items.Count <= 1)
            {
                ShowSuggestionsPopup(false);
            }
        }

        private void FolderNameTbx_TextChanged(object sender, TextChangedEventArgs e)
        {
            string inputText = FolderNameTbx.Text;
            UpdateFullFolderName();

            if (string.IsNullOrWhiteSpace(inputText))
            {
                SetFirstSuggestions();
                return;
            }

            string[] words = inputText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int currentWordIndex = words.Length;

            if (inputText.EndsWith(" "))
            {
                currentWordIndex++;
            }

            int maxSuggestionIndex = _suggestionRules.Keys.DefaultIfEmpty(0).Max();

            if (currentWordIndex > maxSuggestionIndex)
            {
                ShowSuggestionsPopup(false);
                return;
            }

            if (_suggestionRules.TryGetValue(currentWordIndex, out List<string>? suggestionRule))
            {
                if (inputText.EndsWith(" ") && words.Length < currentWordIndex)
                {
                    SuggestionsLbx.ItemsSource = suggestionRule;
                    ShowSuggestionsPopup(true);
                    return;
                }

                string? lastWord = words.LastOrDefault()?.Trim();
                List<string> filteredSuggestions = suggestionRule.Where(s => s.StartsWith(lastWord ?? "", StringComparison.OrdinalIgnoreCase)).ToList();

                if (filteredSuggestions.Count != 0)
                {
                    SuggestionsLbx.ItemsSource = filteredSuggestions;
                    ShowSuggestionsPopup(true);
                    return;
                }
            }

            ShowSuggestionsPopup(false);
        }

        private void NavigateSuggestions(bool isDown)
        {
            _isNavigatingWithArrows = true;

            if (SuggestionsLbx.SelectedIndex == -1)
            {
                SuggestionsLbx.SelectedIndex = isDown ? 0 : SuggestionsLbx.Items.Count - 1;
            }
            else
            {
                SuggestionsLbx.SelectedIndex = isDown
                    ? (SuggestionsLbx.SelectedIndex + 1) % SuggestionsLbx.Items.Count
                    : (SuggestionsLbx.SelectedIndex - 1 + SuggestionsLbx.Items.Count) % SuggestionsLbx.Items.Count;
                SuggestionsLbx.ScrollIntoView(SuggestionsLbx.SelectedItem);
            }

            _isNavigatingWithArrows = false;
        }

        private void SuggestionsLbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isNavigatingWithArrows && SuggestionsLbx.SelectedItem is string selectedSuggestion)
            {
                SelectSuggestion(selectedSuggestion);
                Dispatcher.BeginInvoke(new Action(() => FolderNameTbx.Focus()));
            }
        }

        private void FolderNameTbx_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                if (!SuggestionsPopup.IsOpen)
                {
                    e.Handled = true;
                    return;
                };

                string? selectedSuggestion = SuggestionsLbx.SelectedItem as string;

                if (string.IsNullOrEmpty(selectedSuggestion) && SuggestionsLbx.Items.Count > 0)
                {
                    selectedSuggestion = SuggestionsLbx.Items[0] as string;
                }

                if (!string.IsNullOrEmpty(selectedSuggestion))
                {
                    SelectSuggestion(selectedSuggestion);
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Down || e.Key == Key.Right)
            {
                NavigateSuggestions(true);
                e.Handled = true;
            }
            else if (e.Key == Key.Up || e.Key == Key.Left)
            {
                NavigateSuggestions(false);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                CreateFolder();
                e.Handled = true;
            }
        }

        private void FolderNameTbx_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (invalidFolderNameChars.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        private void FolderNameTbx_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pastedText = (string)e.DataObject.GetData(typeof(string));
                string cleanedText = invalidFolderNameChars.Replace(pastedText, string.Empty);

                if (cleanedText != pastedText)
                {
                    int caretIndex = FolderNameTbx.CaretIndex;
                    FolderNameTbx.Text = FolderNameTbx.Text.Insert(caretIndex, cleanedText);
                    FolderNameTbx.CaretIndex = caretIndex + cleanedText.Length;
                    UpdateFullFolderName();

                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            ShowSuggestionsPopup(false);
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            ShowSuggestionsPopup(false);
        }

        private void CreateBtn_Click(object sender, RoutedEventArgs e)
        {
            CreateFolder();
        }
    }
}
