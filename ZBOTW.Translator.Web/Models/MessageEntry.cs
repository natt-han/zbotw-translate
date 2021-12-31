namespace ZBOTW.Translator.Web.Models
{
    public class MessageText
    {
        public int Line { get; set; }
        public string? OriginalText { get; set; }
        public string? TranslatedText { get; set; }
        public string? Colour { get; set; }
    }
    public class MessageEntry
    {
        public string? EntryName { get; set; }
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
}
