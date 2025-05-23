namespace LogExpert.Core.Entities
{
    [Serializable]
    public class MultiFileOptions
    {
        #region Properties

        public int MaxDayTry { get; set; } = 3;

        public string FormatPattern { get; set; } = "*$J(.)";

        #endregion
    }
}