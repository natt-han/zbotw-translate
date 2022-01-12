using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZBOTW.Translator.Web.Models
{
    public class MessageText
    {
        public int Line { get; set; }
        public string? OriginalText { get; set; }
        public string? TranslatedText { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Colour { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Variable { get; set; }
    }
    public class MessageEntry
    {
        public string? EntryName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NPC { get; set; }
        public List<MessageText> TextList { get; set; }

        public MessageEntry()
        {
            this.EntryName = "";
            this.TextList = new List<MessageText>();
        }
        public MessageEntry(string EntryName)
        {
            this.EntryName = EntryName;
            this.TextList = new List<MessageText>();
        }
    }

    public class MessageTable
    {
        public string FileName { get; set; }
        public List<MessageEntry> EntryList { get; set; }

        public bool IsCompleted { get; set; }

        public MessageTable()
        {
            FileName = "";
            EntryList = new List<MessageEntry>();
        }
        public MessageTable(string fileName)
        {
            FileName = fileName;   
            EntryList = new List<MessageEntry>();
        }
    }

    public class MessageTableInfo
    {
        public string FileName { get; set; }
        public bool IsCompleted { get; set; }

        public int Entry { get; set; }
        public int Text { get; set; }
        public int TranslatedText { get; set; }
    }

    public class ProgressViewModel
    {
        public int TotalText { get; set; }
        public int TotalTranslatedText { get; set; }

        public ProgressViewModel()
        {

        }

        public ProgressViewModel(List<MessageTableInfo> summary)
        {
            this.TotalText = summary.Sum(s => s.Text);
            this.TotalTranslatedText = summary.Sum(s => s.IsCompleted ? s.Text : s.TranslatedText);
        }
    }
}
