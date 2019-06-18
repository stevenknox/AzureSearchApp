using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using static System.Console;
using static System.ConsoleColor;
using System;
using System.IO;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Threading.Tasks;

namespace AzureSearch
{
    public class FileSearch : SearchBase
    {
        private static string storageKey = "";

        public static FileSearch Create() => new FileSearch();

        private FileSearch()
        {
            storageKey = File.ReadAllText($"{AzureCredentialsPath}/storage.private-azure-key");
            index = "azureblob-index";
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
                    u => u.Document.metadata_storage_name,
                    u => u.Document.metadata_content_type
                );

                WriteLine(table);

                WriteLine($"Enter File ID to download:");

                var fileToDownload = Convert.ToInt32(ReadLine());

                DownloadFile($"{Environment.CurrentDirectory}/Downloads", results.Results[fileToDownload - 1].Document.metadata_storage_name);

                WriteLine($"{Environment.NewLine}Enter another search query:");

                Search();
            }
        }

        public DocumentSearchResult<FileIndexModel> Search(string query)
        {
            using (var client = CreateIndexClient())
            {
               return GetSearchResults(client, query);
            }
        }
        private  DocumentSearchResult<FileIndexModel> GetSearchResults(SearchIndexClient client, string query)
        {
            var searchParams = new SearchParameters
            {
                IncludeTotalResultCount = true
            };

            try
            {
                return client.Documents.Search<FileIndexModel>(query, searchParams);
            }
            catch (System.Exception ex)
            {
                WriteLine($"Search failed with error {ex.Message}");
            }
            return null;
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
                if(!File.Exists(filePath))
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