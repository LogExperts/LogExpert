using LogExpert.Core.Classes.Filter;

namespace LogExpert.Core.Classes.Persister
{
    public class FilterTabData
    {
        public FilterParams FilterParams { get; set; } = new();

        public PersistenceData PersistenceData { get; set; } = new();
    }
}