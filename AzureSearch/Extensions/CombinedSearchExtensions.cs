using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Search.Models;

namespace AzureSearch
{
    public static class CombinedSearchExtensions
    {
        public static IEnumerable<CombinedSearch> ApplyIndex(this IEnumerable<CombinedSearch> results, int startingIndex = 0) => results.Select((value, i) => { value.Index = i + startingIndex; return value; });

        public static List<CombinedSearch> ToCombinedSearch(this DocumentSearchResult<SearchIndex> results)
        {
            var search = new List<CombinedSearch>();
            foreach (var item in results.Results)
            {
                search.Add(new CombinedSearch(item.Document.Id, 
                                            item.Document.Name, 
                                            item.Document.DataType == "Object" ? item.Document.Model : item.Document.DataType, 
                                            item.Document.ToFriendlyFileType(), 
                                            item.Document.DataType.ToSearchType(), 
                                            item.Document.ToDetails()));
            }
            return search;
        }

        public static string ToDetails(this SearchIndex item)
        {
            if(!string.IsNullOrWhiteSpace(item.Url))
                return item.Url;
            else if(item.DataType == "Object")
                return item.MergedText;
            else
                return item.Name;
        }
        public static string ToFriendlyFileType(this SearchIndex input)
        {
           switch (input.DataType)
           {
               case "Object": return input.Model.Split('.')?.Last();
               case "image/jpeg": return "Image (jpg)";
               case "image/png": return "Image (png)";
               case "image/gif": return "Image (gif)";
               case "application/pdf": return "PDF";
               case "application/vnd.openxmlformats-officedocument.wordprocessingml.document": return "Word";
               case "application/vnd.openxmlformats-officedocument.presentationml.presentation": return "Powerpoint";
               case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": return "Excel";
               case "video/mp4": return "Video (mp4)";
               case "text/html": return "HTML";

               default: return input.DataType;
           }
        }

        public static SearchType ToSearchType(this string input)
        {
           switch (input)
           {
               case "Object": return SearchType.Object;
               case "Media": return SearchType.Media;

               default: return SearchType.File;
           }
        }
    }
}