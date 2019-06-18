using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Search.Models;

namespace AzureSearch
{

    public static class CombinedSearchExtensions
    {
        public static IEnumerable<CombinedSearch> ApplyIndex(this IEnumerable<CombinedSearch> results, int startingIndex = 0) => results.Select((value, i) => { value.Id = i + startingIndex; return value; });
        public static List<CombinedSearch> ToCombinedSearch(this DocumentSearchResult<AreaOfInterestIndexModel> results)
        {
            var search = new List<CombinedSearch>();
            foreach (var item in results.Results)
            {
                search.Add(new CombinedSearch(item.Document.Name, "Database", SearchType.Text, 
                    new List<AreaOfInterestIndexModel> { item.Document }.ToStringTable(u => u.Name,
                                                                                        u => u.Address,
                                                                                        u => u.Town,
                                                                                        u => u.Postcode,
                                                                                        u => u.Facility)));
            }
            return search;
        }

        public static List<CombinedSearch> ToCombinedSearch(this DocumentSearchResult<FileIndexModel> results)
        {
            var search = new List<CombinedSearch>();
            foreach (var item in results.Results)
            {
                search.Add(new CombinedSearch(item.Document.metadata_storage_name, item.Document.ToFriendlyFileType(), SearchType.File, item.Document.metadata_storage_name));
            }
            return search;
        }

        public static List<CombinedSearch> ToCombinedSearch(this DocumentSearchResult<MediaIndexModel> results)
        {
            var search = new List<CombinedSearch>();
            foreach (var item in results.Results)
            {
                search.Add(new CombinedSearch(item.Document.Title, "Video (mp4) with transcript", SearchType.Media, item.Document.Url));
            }
            return search;
        }


        public static string ToFriendlyFileType(this FileIndexModel input)
        {
           switch (input.metadata_content_type)
           {
               case "image/jpeg": return "Image (jpg)";
               case "image/png": return "Image (png)";
               case "image/gif": return "Image (gif)";
               case "application/pdf": return "PDF";
               case "application/vnd.openxmlformats-officedocument.wordprocessingml.document": return "Word";
               case "application/vnd.openxmlformats-officedocument.presentationml.presentation": return "Powerpoint";
               case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": return "Excel";
               case "video/mp4": return "Video (mp4)";
               case "text/html": return "HTML";

               default: return input.metadata_content_type;
           }
        }
    }
}