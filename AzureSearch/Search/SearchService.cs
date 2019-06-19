using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using static System.Console;
using static System.ConsoleColor;

namespace AzureSearch
{
    public interface ISearchService
    {
        DocumentSearchResult<SearchIndex> Search(string query, string filter = "");
        Task Reindex(string basePath, Action<string> onEventAdded);
    }
    public class SearchService : SearchBase, ISearchService
    {
        private static readonly int indexOffset = 1;
        private IDisposable eventAddedSubscription;

        public static SearchService Create() => new SearchService();

        public SearchService()
        {
             
        }

        public void StartSearch()
        {
            WriteLine($"Enter Your Search Query:");

            var query = ReadLine();

            var results = Search(query)
                           .ToCombinedSearch()
                           .ApplyIndex(startingIndex: indexOffset)
                           .ToList();

            ForegroundColor = Green;

            var resultsCount = results.Count;

            WriteLine($"{Environment.NewLine}{resultsCount} results found for : {query}{Environment.NewLine}");

            ResetColor();

            var table = results.ToStringTable(
                u => u.Id,
                u => u.Name,
                u => u.DisplayType
            );

            WriteLine(table);

            WriteLine($"{Environment.NewLine}Enter an ID from the results to view, or enter 0 to start a new search");

            var input = ReadLine();

            if (Int32.TryParse(input, out int inputOption))
            {
                if (inputOption == 0)
                {
                    StartSearch();
                }
                else
                {
                    CombinedSearch item = results[inputOption - indexOffset];

                    if (item.SearchType == SearchType.File)
                    {
                        var file = FileSearch.Create().DownloadFile($"{basePath}/Downloads", item.Name);

                        WriteLine($"Opening File from {file}");

                        try
                        {
                            Process.Start(@"cmd.exe ", $@"/c ""{file}""");
                        }
                        catch (System.ComponentModel.Win32Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    else if (item.SearchType == SearchType.Media)
                    {
                        WriteLine($"Launching Video");

                        try
                        {
                            Process.Start(@"cmd.exe ", $@"/c ""{basePath}/Media/Output/{item.Details}""");
                        }
                        catch (System.ComponentModel.Win32Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                    }
                    else if (item.SearchType == SearchType.Object)
                    {
                        WriteLine(PrintObject(item));
                    }

                    StartSearch();
                }
            }
            else
            {
                StartSearch();
            }

        }

        public static string PrintObject(CombinedSearch item, bool outputAsHtml = false)
        {
            //we are assuming the type is in the same assembly, otherwise you would need to ensure namespace prefix and the assembly is loaded
            Type castToType = Type.GetType(item.Type);

            dynamic myObject = castToType == null ? JsonConvert.DeserializeObject(item.Details) : JsonConvert.DeserializeObject(item.Details, castToType);

            return outputAsHtml ? ObjectDumper.DumpHtml(myObject) : ObjectDumper.Dump(myObject);
        }

        public DocumentSearchResult<SearchIndex> Search(string query, string filter = "")
        {
            using (var client = CreateIndexClient())
            {
                return GetSearchResults(client, query, filter);
            }
        }

        private DocumentSearchResult<SearchIndex> GetSearchResults(SearchIndexClient client, string query, string filter = "")
        {
            var searchParams = new SearchParameters
            {
                // Select = new[] { "name", "address", "town", "postcode" },
                // Facets = new[] { "town" }.ToList(),
                // Filter = AppendFilters(filter), 
                IncludeTotalResultCount = true
            };

            return client.Documents.Search<SearchIndex>(query, searchParams);
        }

        private string AppendFilters(string filter)
        {
            if (!string.IsNullOrEmpty(filter))
                return filter;

            return "";
        }

        public async Task Reindex(string basePath, Action<string> onEventAdded)
        {
            var textSearch = TextSearch.Create(basePath);

            eventAddedSubscription = textSearch.EventAdded.Subscribe(onEventAdded);

            await textSearch.Index();

            await FileSearch
                    .Create(basePath)
                    .Index();
            await MediaSearch
                    .Create(basePath)
                    .Index();
        }

         public void Dispose()
        {
            eventAddedSubscription.Dispose();
        }
    }
}