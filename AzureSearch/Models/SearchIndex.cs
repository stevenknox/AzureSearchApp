using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace AzureSearch
{
    [SerializePropertyNamesAsCamelCase]
    public class SearchIndex
    {
        [System.ComponentModel.DataAnnotations.Key]
        public string Id { get; set; }
        [IsFilterable, IsSortable, IsSearchable]
        public string Name { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable]
        public string DataType { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable]
        public string Model { get; set; }
        [IsSearchable]
        public string MergedText { get; set; }
        // [IsSearchable]
        // public string Content { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] KeyPhrases { get; set; }

        [IsSearchable]
        public string Url { get; set; }
        // public string Enriched { get; set; }



    }
}