namespace LogExpert.Core.Entities
{
    [Serializable]
    public class SearchParams
    {
        public int CurrentLine { get; set; }

        public List<string> HistoryList { get; set; } = [];

        public bool IsCaseSensitive { get; set; } = false;

        public bool IsFindNext { get; set; }

        public bool IsForward { get; set; } = true;

        public bool IsFromTop { get; set; } = false;

        public bool IsRegex { get; set; } = false;

        public string SearchText { get; set; } = string.Empty;

        [field: NonSerialized]
        public bool IsShiftF3Pressed { get; set; }
    }
}