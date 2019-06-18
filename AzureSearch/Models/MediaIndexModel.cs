using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace AzureSearch
{
    [SerializePropertyNamesAsCamelCase]
    public class MediaIndexModel
    {
        [System.ComponentModel.DataAnnotations.Key]
        [IsFilterable]
        public string Id { get; set; }
        [IsFilterable, IsSortable, IsSearchable]
        public string Title { get; set; }
        [IsSearchable]
        public string Transcribed_text { get; set; }

        public string Url { get; set; }
    }
}