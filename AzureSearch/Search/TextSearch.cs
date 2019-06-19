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
        public static TextSearch Create(string rootPath = "") => new TextSearch { basePath = string.IsNullOrWhiteSpace(rootPath) ? Environment.CurrentDirectory : rootPath };

        private TextSearch()
        {
        }

        public async Task Index()
        {
            using (var client = CreateServiceClient())
            {
                if (await client.Indexes.ExistsAsync(index) == false)
                {
                    RaiseEvent("Creating Index");
                    CreateIndex(client);
                }
                else
                {
                    RaiseEvent("Index Exists");
                }

                var data = await GetData();

                var actions = new List<IndexAction<SearchIndex>>();
                foreach (var item in data)
                {
                    actions.Add(IndexAction.Upload(item));
                    RaiseEvent($"Adding {item.Name} to Azure Search");
                }

                var batch = IndexBatch.New(actions);

                try
                {
                    RaiseEvent($"Uploading Indexes to Azure Search");
                    ISearchIndexClient indexClient = client.Indexes.GetClient(index);
                    indexClient.Documents.Index(batch);
                    RaiseEvent($"Indexing Complete");
                }
                catch (Exception ex)
                {
                    RaiseEvent("Indexing Failed" + ex.ToString());
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
            var data = await File.ReadAllTextAsync($"{basePath}/SeedData/Data.json");

            return JsonConvert.DeserializeObject<AreasOfInterest>(data).ToSearchIndex();
        }

        private async Task<List<SearchIndex>> GetPrivateData()
        {
            var courses = await File.ReadAllTextAsync($"{basePath}/SeedData/Private/Courses.json");

            var courseIndex = JsonConvert.DeserializeObject<AreasOfInterest>(courses).ToSearchIndex();
        }

        private void RaiseEvent(string message)
        {
            try
            {
                _eventAdded.OnNext(message);
            }
            catch (Exception ex)
            {
                _eventAdded.OnError(ex);
            }
        }
    }
}