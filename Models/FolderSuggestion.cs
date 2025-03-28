namespace Numbered_Folder.Models
{
    public class FolderSuggestion
    {
        /// <summary>
        /// Название папки (&папка)
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// Правила (1%подсказка, 2%подсказка, 3%подсказка)
        /// </summary>
        public Dictionary<int, List<string>> Rules { get; set; } = [];
    }
}
