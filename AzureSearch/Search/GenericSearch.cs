using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using static System.Console;
using static System.ConsoleColor;

namespace AzureSearch
{
    public class GenericSearch : SearchBase
    {
        private static string storageKey = "";
        private static string azureFunctionsEndpoint = "";
        private const string dataSourceName = "search-blobstorage-data";
        private const string skillName = "generic-search-skills";
        private const string indexerName = "generic-search-indexer";

        public static GenericSearch Create() => new GenericSearch();

        private GenericSearch()
        {
            storageKey = File.ReadAllText($"{AzureCredentialsPath}/storage.private-azure-key");
            azureFunctionsEndpoint = File.ReadAllText($"{AzureCredentialsPath}/functions.azure-endpoint");
            index = "generic-index";
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
                    u => u.Document.DataType
                );

                WriteLine(table);

                WriteLine($"Enter File ID to download:");

                var fileToDownload = Convert.ToInt32(ReadLine());

                DownloadFile($"{Environment.CurrentDirectory}/Downloads", results.Results[fileToDownload - 1].Document.Name);

                WriteLine($"{Environment.NewLine}Enter another search query:");

                Search();
            }
        }

        public DocumentSearchResult<GenericIndexModel> Search(string query)
        {
            using (var client = CreateIndexClient())
            {
                return GetSearchResults(client, query);
            }
        }
        private DocumentSearchResult<GenericIndexModel> GetSearchResults(SearchIndexClient client, string query)
        {
            var searchParams = new SearchParameters
            {
                // Select = new[] { "keyphrases" },
                IncludeTotalResultCount = true
            };

            try
            {
                return client.Documents.Search<GenericIndexModel>(query, searchParams);
            }
            catch (System.Exception ex)
            {
                WriteLine($"Search failed with error {ex.Message}");
            }
            return null;
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

                var indexBlobStorageSuccess = IndexBlobStorage(client);

                // var data = await GetData();

                // var actions = new List<IndexAction<GenericIndexModel>>();
                // foreach (var item in data)
                // {
                //     actions.Add(IndexAction.Upload(item));
                //     WriteLine($"Adding {item.Name} to Azure Search");
                // }

                // var batch = IndexBatch.New(actions);

                // try
                // {
                //     WriteLine($"Uploading Indexes to Azure Search");
                //     ISearchIndexClient indexClient = client.Indexes.GetClient(index);
                //     indexClient.Documents.Index(batch);
                //     WriteLine($"Indexing Complete");
                // }
                // catch (Exception ex)
                // {
                //     WriteLine("Indexing Failed" + ex.ToString());
                // }
            }
        }

        private void CreateIndex(SearchServiceClient client)
        {
            var def = new Index()
            {
                Name = index,
                Fields = FieldBuilder.BuildForType<GenericIndexModel>()
            };

            try
            {
                client.Indexes.Create(def);
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to Create Index: {ex.ToString()}");
            }

        }

        public bool IndexBlobStorage(SearchServiceClient client)
        {
            CreateBlobStorageDataSource(client);
            CreateBlogStorageIndexSkills(client);
            CreateIndexer(client);

            return IndexBlobStorageStatus(client);

        }

        private bool IndexBlobStorageStatus(SearchServiceClient client)
        {
            try
            {
                IndexerExecutionInfo demoIndexerExecutionInfo = client.Indexers.GetStatus(indexerName);

                switch (demoIndexerExecutionInfo.Status)
                {
                    case IndexerStatus.Error:
                        WriteLine("Indexer has error status");
                        return false;
                    case IndexerStatus.Running:
                        WriteLine("Indexer is running");
                        System.Threading.Thread.Sleep(5000);
                        return IndexBlobStorageStatus(client);
                    case IndexerStatus.Unknown:
                        WriteLine("Indexer status is unknown");
                        return false;
                    default:
                        WriteLine("No indexer information. Assumign Completed");
                        return true;
                }
            }
            catch (Exception ex)
            {
                WriteLine("Indexer Failed - " + ex.ToString());
                return false;
            }
        }

        public void CreateBlobStorageDataSource(SearchServiceClient client)
        {
            DataSource dataSource = DataSource.AzureBlobStorage(
                name: dataSourceName,
                storageConnectionString: storageKey,
                containerName: "search-data",
                description: "Search Static Assets uploaded to Blog Storage");

            try
            {
                client.DataSources.CreateOrUpdate(dataSource);
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to Create Datasource: {ex.ToString()}");
            }
        }

        public void CreateBlogStorageIndexSkills(SearchServiceClient client)
        {
            List<Skill> skills = new List<Skill>
            {
                OcrSkill(),
                MergeSkill(),
                KeyPhraseExtractionSkill(),
                // CustomSkill()
            };

            Skillset skillset = new Skillset(
                name: skillName,
                description: "Generic Search skillset",
                skills: skills);

            try
            {
                client.Skillsets.CreateOrUpdate(skillset);
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to Create Skills: {ex.ToString()}");
            }

            OcrSkill OcrSkill()
            {
                List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>
                {
                    new InputFieldMappingEntry(
                    name: "image",
                    source: "/document/normalized_images/*")
                };

                List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>
                {
                    new OutputFieldMappingEntry(
                    name: "text",
                    targetName: "text")
                };

                return new OcrSkill(
                    description: "Extract text (plain and structured) from image",
                    context: "/document/normalized_images/*",
                    inputs: inputMappings,
                    outputs: outputMappings,
                    defaultLanguageCode: OcrSkillLanguage.En,
                    shouldDetectOrientation: true);
            }

            MergeSkill MergeSkill()
            {
                List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>
                {
                    new InputFieldMappingEntry(
                    name: "text",
                    source: "/document/content"),
                    new InputFieldMappingEntry(
                    name: "itemsToInsert",
                    source: "/document/normalized_images/*/text"),
                    new InputFieldMappingEntry(
                    name: "offsets",
                    source: "/document/normalized_images/*/contentOffset")
                };

                List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>
                {
                    new OutputFieldMappingEntry(
                    name: "mergedText",
                    targetName: "merged_text")
                };

                return new MergeSkill(
                    description: "Create merged_text which includes all the textual representation of each image inserted at the right location in the content field.",
                    context: "/document",
                    inputs: inputMappings,
                    outputs: outputMappings,
                    insertPreTag: " ",
                    insertPostTag: " ");
            }

            KeyPhraseExtractionSkill KeyPhraseExtractionSkill()
            {
                List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>
                {
                    new InputFieldMappingEntry(
                    name: "text",
                    source: "/document/pages/*")
                };

                List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>
                {
                    new OutputFieldMappingEntry(
                    name: "keyPhrases",
                    targetName: "keyPhrases")
                };

                return new KeyPhraseExtractionSkill(
                    description: "Extract the key phrases",
                    context: "/document/pages/*",
                    inputs: inputMappings,
                    outputs: outputMappings);
            }

            // WebApiSkill CustomSkill()
            // {
            //     List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>
            //     {
            //         new InputFieldMappingEntry(
            //         name: "contentType",
            //         source: "/document/pages/*") //"metadata_storage_content_type"
            //     };

            //     List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>
            //     {
            //         new OutputFieldMappingEntry(
            //         name: "dataType",
            //         targetName: "dataType")
            //     };

            //     var s = new WebApiSkill(
            //         uri: azureFunctionsEndpoint,
            //         context: "/document",
            //         description: "Get File Information from MimeType",
            //         inputs: inputMappings,
            //         outputs: outputMappings,
            //         batchSize: 1,
            //         httpHeaders: new WebApiHttpHeaders(headers: new Dictionary<string, string>()),
            //         httpMethod: "POST");

            //     try
            //     {
            //         s.Validate();

            //         return s;
            //     }
            //     catch (System.Exception ex)
            //     {
            //         WriteLine(ex.ToString());
            //         throw ex;
            //     }

            // }
        }

        private void CreateIndexer(SearchServiceClient client)
        {
            IDictionary<string, object> config = new Dictionary<string, object>();
            config.Add(
                key: "dataToExtract",
                value: "contentAndMetadata");
            config.Add(
                key: "imageAction",
                value: "generateNormalizedImages");

            List<FieldMapping> fieldMappings = new List<FieldMapping>
            {
                new FieldMapping(
                    sourceFieldName: "metadata_storage_name",
                    targetFieldName: "name"),
                new FieldMapping(
                    sourceFieldName: "metadata_content_type",
                    targetFieldName: "dataType"),
                new FieldMapping(
                    sourceFieldName: "metadata_storage_path",
                    targetFieldName: "id",
                    mappingFunction: new FieldMappingFunction(
                    name: "base64Encode")),
                new FieldMapping(
                    sourceFieldName: "content",
                    targetFieldName: "data")
            };

            List<FieldMapping> outputMappings = new List<FieldMapping>
            {
                new FieldMapping(
                sourceFieldName: "/document/pages/*/keyPhrases/*",
                targetFieldName: "tags")
            };

            Indexer indexer = new Indexer(
                name: indexerName,
                dataSourceName: dataSourceName,
                targetIndexName: index,
                description: "Generic Search Indexer",
                skillsetName: skillName,
                parameters: new IndexingParameters(
                    maxFailedItems: -1,
                    maxFailedItemsPerBatch: -1,
                    configuration: config),
                fieldMappings: fieldMappings,
                outputFieldMappings: outputMappings);

            try
            {
                bool exists = client.Indexers.Exists(indexer.Name);

                if (exists)
                {
                    client.Indexers.Delete(indexer.Name);
                }

                client.Indexers.Create(indexer);
            }
            catch (Exception ex)
            {
                WriteLine("Create Indexer Failed:" + ex);
            }
        }

        private async Task<List<GenericIndexModel>> GetData()
        {
            var data = await File.ReadAllTextAsync($"{Environment.CurrentDirectory}/GenericData.json");

            return JsonConvert.DeserializeObject<List<GenericIndexModel>>(data);
        }

        public string DownloadFile(string targetFolder, string fileName)
        {
            if (CloudStorageAccount.TryParse(storageKey, out CloudStorageAccount storageAccount))
            {
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("search-data");

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

                var filePath = $"{targetFolder}/{fileName}";
                if (!File.Exists(filePath))
                {
                    WriteLine($"Downloading {fileName} from Azure");

                    using (var filestream = File.OpenWrite(filePath))
                    {
                        blockBlob.DownloadToStream(filestream);
                    }
                }

                return filePath;
            }
            else
            {
                throw new ArgumentNullException("Storage Key Not Found!");
            }
        }
    }
}