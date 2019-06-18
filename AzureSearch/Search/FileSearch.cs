using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using static System.Console;
using static System.ConsoleColor;
using System;
using System.IO;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AzureSearch
{
    public class FileSearch : SearchBase
    {
        private static string storageKey = "";
        private const string dataSourceName = "search-blobstorage-data";
        private const string skillName = "generic-search-skills";
        private const string indexerName = "generic-search-indexer";

        public static FileSearch Create() => new FileSearch();

        private FileSearch()
        {
            storageKey = File.ReadAllText($"{AzureCredentialsPath}/storage.private-azure-key");
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

                await IndexBlobStorage(client);

                WriteLine("Indexing Complete");
            }
        }


        private void CreateIndex(SearchServiceClient client)
        {
            var def = new Index()
            {
                Name = index,
                Fields = FieldBuilder.BuildForType<SearchIndex>()
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

        public async Task<bool> IndexBlobStorage(SearchServiceClient client)
        {
            await CreateBlobStorageDataSource(client);
            await CreateBlogStorageIndexSkills(client);
            await CreateIndexer(client);

            return IndexBlobStorageStatus(client);

        }

        public async Task Delete()
        {
            using (var client = CreateServiceClient())
            {

                if (await client.Indexers.ExistsAsync(indexerName))
                {
                    WriteLine($"Indexer {indexerName} exists");

                    await client.Indexers.DeleteAsync(indexerName);

                    WriteLine($"Deleted Indexer {indexerName}");
                }
                if (await client.Skillsets.ExistsAsync(skillName))
                {
                    WriteLine($"Skillset {skillName} exists");

                    await client.Skillsets.DeleteAsync(skillName);

                    WriteLine($"Deleted Skillset {skillName}");
                }
                if (await client.DataSources.ExistsAsync(dataSourceName))
                {
                    WriteLine($"Datasource {dataSourceName} exists");

                    await client.DataSources.DeleteAsync(dataSourceName);

                    WriteLine($"Deleted Datasource {dataSourceName}");
                }
                 if (await client.Indexes.ExistsAsync(index))
                {
                    WriteLine($"Index {index} exists");

                    await client.Indexes.DeleteAsync(index);

                    WriteLine($"Deleted Index {index}");
                }
            }
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
                        if (demoIndexerExecutionInfo.LastResult.Status == IndexerExecutionStatus.Success)
                        {
                            return true;
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(5000);
                            return IndexBlobStorageStatus(client);
                        }

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

        public async Task CreateBlobStorageDataSource(SearchServiceClient client)
        {
            if (await client.DataSources.ExistsAsync(dataSourceName))
            {
                WriteLine($"Datasource {dataSourceName} exists");
            }
            else
            {
                DataSource dataSource = DataSource.AzureBlobStorage(
                name: dataSourceName,
                storageConnectionString: storageKey,
                containerName: "search-data",
                description: "Search Static Assets uploaded to Blog Storage");

                try
                {
                    WriteLine($"Creating Datasource {dataSourceName}");
                    client.DataSources.CreateOrUpdate(dataSource);
                }
                catch (Exception ex)
                {
                    WriteLine($"Failed to Create Datasource: {ex.ToString()}");
                }
            }

        }

        public async Task CreateBlogStorageIndexSkills(SearchServiceClient client)
        {
            if (await client.Skillsets.ExistsAsync(skillName))
            {
                WriteLine($"Skillset {dataSourceName} exists");
            }
            else
            {
                List<Skill> skills = new List<Skill>
                {
                    OcrSkill(),
                    MergeSkill(),
                    KeyPhraseExtractionSkill()
                    // CustomSkill()
                };

                Skillset skillset = new Skillset(
                    name: skillName,
                    description: "Generic Search skillset",
                    skills: skills);

                try
                {
                    WriteLine($"Creating Skillset {skillName}");
                    client.Skillsets.CreateOrUpdate(skillset);
                }
                catch (Exception ex)
                {
                    WriteLine($"Failed to Create Skills: {ex.ToString()}");
                }
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
                    targetName: "mergedText")
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
                    source: "/document/mergedText")
                };

                List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>
                {
                    new OutputFieldMappingEntry(
                    name: "keyPhrases",
                    targetName: "keyPhrases")
                };

                return new KeyPhraseExtractionSkill(
                    description: "Extract the key phrases",
                    context: "/document/mergedText",
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

        private async Task CreateIndexer(SearchServiceClient client)
        {
            if (await client.Indexers.ExistsAsync(indexerName))
            {
                WriteLine($"Indexer {indexerName} exists");

                await client.Indexers.RunAsync(indexerName);

                WriteLine($"Running Indexer {indexerName}. This may take a while to complete depending on how number of files being indexed.");
            }
            else
            {
                //Full list of params here: https://docs.microsoft.com/en-gb/rest/api/searchservice/create-indexer#indexer-parameters
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
                        name: "base64Encode"))
                };

                List<FieldMapping> outputMappings = new List<FieldMapping>
                {
                    new FieldMapping(
                    sourceFieldName: "/document/mergedText/keyPhrases/*",
                    targetFieldName: "keyPhrases"),
                      new FieldMapping(
                    sourceFieldName: "/document/mergedText",
                    targetFieldName: "mergedText")
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
                    WriteLine($"Creating Indexer {skillName}. Please wait while everything provisions..");
                    client.Indexers.Create(indexer);
                    System.Threading.Thread.Sleep(5000);
                }
                catch (Exception ex)
                {
                    WriteLine("Create Indexer Failed:" + ex);
                }
            }
        }


        public async Task UploadFileToStorage()
        {
            WriteLine($"Enter full path of file you wish to upload:");

            var path = ReadLine();

            await UploadFile(path);
        }

        private async Task UploadFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            if (CloudStorageAccount.TryParse(storageKey, out CloudStorageAccount storageAccount))
            {
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("search-data");

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

                WriteLine($"Uploading {fileName} to Azure Blob Storage");

                await blockBlob.UploadFromFileAsync(filePath);

                WriteLine($"Upload Complete");
            }
            else
            {
                throw new ArgumentNullException("Storage Key Not Found!");
            }
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