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
    public class TextSearch : SearchBase
    {
        public static TextSearch Create() => new TextSearch();

        private TextSearch()
        {
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

                var actions = new List<IndexAction<SearchIndex>>();
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
                Fields = FieldBuilder.BuildForType<SearchIndex>()
            };

            client.Indexes.Create(def);
        }

        private async Task<List<SearchIndex>> GetData()
        {
            var data = await File.ReadAllTextAsync($"{Environment.CurrentDirectory}/Data.json");

            return JsonConvert.DeserializeObject<AreasOfInterest>(data).ToSearchIndex();
        }
    }
}