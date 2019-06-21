namespace AzureSearch
{
    public class CombinedSearch
    {
        public CombinedSearch()
        {
            
        }
        public CombinedSearch(string id, string name, string type, string displayType, SearchType searchType, string details = "")
        {
            Id = id;
            Name = name;
            Type = type;
            DisplayType = displayType;
            Details = details;
            SearchType = searchType;
        }
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayType { get; set; }
        public string Type { get; set; }
        public SearchType SearchType { get; set; }
        public string Details { get; set; }
        public int Index { get; set; }
    }
}