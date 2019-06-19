namespace AzureSearch
{
    public class CombinedSearch
    {
        public CombinedSearch()
        {
            
        }
        public CombinedSearch(string name, string type, string displayType, SearchType searchType, string details = "")
        {
            Name = name;
            Type = type;
            DisplayType = displayType;
            Details = details;
            SearchType = searchType;
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayType { get; set; }
        public string Type { get; set; }
        public SearchType SearchType { get; set; }
        public string Details { get; set; }
    }
}