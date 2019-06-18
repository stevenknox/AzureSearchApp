using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using static System.Console;
using static System.ConsoleColor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace AzureSearch
{
    public class TextSearch : AzureSearch.SearchBase
    {
        public static TextSearch Create() => new TextSearch();

        private TextSearch()
        {
            index = "areas-of-interest";
        }

        public void Search()
        {
            using (var client = CreateIndexClient())
            {
                WriteLine($"Enter Your Search Query:");

                var query = ReadLine();

                var results = GetSearchResults(client, query);

                ForegroundColor = Green;

                WriteLine($"{Environment.NewLine}{results.Count} results found for : {query}{Environment.NewLine}");

                ResetColor();

                var table = results.Results.ToStringTable(
                    u => u.Document.Name,
                    u => u.Document.Address,
                    u => u.Document.Town,
                    u => u.Document.Postcode,
                    u => u.Document.Facility
                );

                WriteLine(table);

                WriteLine($"{Environment.NewLine}Enter another search query:");

                Search();
            }
        }
        public DocumentSearchResult<AreaOfInterestIndexModel> Search(string query, string filter = "")
        {
            using (var client = CreateIndexClient())
            {
               return GetSearchResults(client, query, filter);
            }
        }

        private DocumentSearchResult<AreaOfInterestIndexModel> GetSearchResults(SearchIndexClient client, string query, string filter = "")
        {
            var searchParams = new SearchParameters
            {
                Select = new[] { "name", "address", "town", "postcode" },
                Facets = new[] { "town" }.ToList(),
                Filter = AppendFilters(filter), 
                IncludeTotalResultCount = true
            };

            return client.Documents.Search<AreaOfInterestIndexModel>(query, searchParams);
        }

        private string AppendFilters(string filter)
        {
            if(!string.IsNullOrEmpty(filter))
                return filter;

            return "";
        }

        public async Task Index()
        {
            using (var client = CreateServiceClient())
            {
                if (await client.Indexes.ExistsAsync(index) == false)
                {
                    WriteLine("Creating Index");
                    CreateIndex(client);
                }
                else
                {
                    WriteLine("Index Exists");
                }

                var data = await GetData();

                var actions = new List<IndexAction<AreaOfInterestIndexModel>>();
                foreach (var item in data)
                {
                    actions.Add(IndexAction.Upload(item));
                    WriteLine($"Adding {item.Name} to Azure Search");
                }

                var batch = IndexBatch.New(actions);

                try
                {
                    WriteLine($"Uploading Indexes to Azure Search");
                    ISearchIndexClient indexClient = client.Indexes.GetClient(index);
                    indexClient.Documents.Index(batch);
                    WriteLine($"Indexing Complete");
                }
                catch (Exception ex)
                {
                    WriteLine("Indexing Failed" + ex.ToString());
                }
            }
        }

        private void CreateIndex(SearchServiceClient client)
        {
            var def = new Index()
            {
                Name = index,
                Fields = FieldBuilder.BuildForType<AreaOfInterestIndexModel>()
            };

            client.Indexes.Create(def);
        }

        private async Task<List<AreaOfInterestIndexModel>> GetData()
        {
            var data = await File.ReadAllTextAsync($"{Environment.CurrentDirectory}/Data.json");

            return JsonConvert.DeserializeObject<AreasOfInterest>(data).ToAreaOfInterestList();
        }
    }
}