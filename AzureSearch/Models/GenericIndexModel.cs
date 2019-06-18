using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace AzureSearch
{
    [SerializePropertyNamesAsCamelCase]
    public class GenericIndexModel
    {
        [System.ComponentModel.DataAnnotations.Key]
        public string Id { get; set; }
        [IsFilterable, IsSortable, IsSearchable]

        public string Name { get; set; }
        [IsFilterable, IsFacetable]
        public string[] Tags { get; set; }

        [IsSearchable]
        public string Data { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable]
        public string DataType { get; set; }
        
        [IsFilterable, IsSortable, IsSearchable, IsFacetable]
        public string Model { get; set; }
        
    }
}