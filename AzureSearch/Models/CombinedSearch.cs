namespace AzureSearch
{
    public class CombinedSearch
    {
        public CombinedSearch(string name, string type, SearchType searchType, string url = "")
        {
            Name = name;
            Type = type;
            Details = url;
            SearchType = searchType;
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public SearchType SearchType { get; set; }
        public string Details { get; set; }
    }
}