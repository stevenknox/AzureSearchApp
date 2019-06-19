using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Console;
using static System.ConsoleColor;

namespace AzureSearch
{
    public class MediaSearch : SearchBase
    {
        private readonly MediaServicesAuth mediaServicesAuth;
        private static readonly XNamespace ttmlns = "http://www.w3.org/ns/ttml";
        private string mediaFolder;
        private string mediaFolderWithoutRoot;
        private string mediaOutputFolder = "";

        public static MediaSearch Create(string rootPath = "")
        {
            var setBasePath = string.IsNullOrWhiteSpace(rootPath) ? Environment.CurrentDirectory : rootPath;
            var setMediaFolder = $"{setBasePath}{Path.DirectorySeparatorChar}Media";
            var setMediaOutputFolder = $"{setMediaFolder}{Path.DirectorySeparatorChar}Output";

            return new MediaSearch 
            { 
                basePath = setBasePath,
                mediaFolder = setMediaFolder,
                mediaOutputFolder = setMediaOutputFolder,
                mediaFolderWithoutRoot = $"{Path.DirectorySeparatorChar}Media{Path.DirectorySeparatorChar}Output{Path.DirectorySeparatorChar}",
            };
        }

        private MediaSearch()
        {
            mediaServicesAuth = JsonConvert.DeserializeObject<MediaServicesAuth>(File.ReadAllText($"{AzureCredentialsPath}/media.private-azure-key"));
        }

        public string TTML(string rootFolder, string fileName) => File.ReadAllText($"{rootFolder}{mediaFolderWithoutRoot}{fileName}{Path.DirectorySeparatorChar}transcript.ttml");
        public string VTT(string rootFolder, string fileName) => File.ReadAllText($"{rootFolder}{mediaFolderWithoutRoot}{fileName}{Path.DirectorySeparatorChar}transcript.ttml");
        public string Transcript(string rootFolder, string fileName) => ParseTTML($"{rootFolder}{mediaFolderWithoutRoot}{fileName}{Path.DirectorySeparatorChar}transcript.ttml");

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

                var actions = new List<IndexAction<SearchIndex>>();
                foreach (var item in GetData())
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
                catch (System.Exception ex)
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

        private List<SearchIndex> GetData()
        {
            var models = new List<SearchIndex>();

            var mediaFolders = Directory.EnumerateDirectories($"{mediaOutputFolder}", "*.*", SearchOption.TopDirectoryOnly);

            foreach (var media in mediaFolders)
            {
                var model = new SearchIndex
                {
                    Name = Path.GetFileName(media),
                    Url = $"{Path.GetFileName(media)}.mp4",
                    Id = ConvertToAlphaNumeric(Path.GetFileName(media)).ToLower().Replace(" ", ""),
                    MergedText = ParseTTML($"{media}{Path.DirectorySeparatorChar}transcript.ttml"),
                    DataType = "Media"
                };
                models.Add(model);
            }
            return models;
        }

        static string ConvertToAlphaNumeric(string plainText)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            return rgx.Replace(plainText, "");
        }
        private static string ParseTTML(string ttmlFile)
        {
            // This will extract all the spoken text from a TTML file into a single string
            return string.Join("\r\n", XDocument.Load(ttmlFile)
                                                .Element(ttmlns + "tt")
                                                .Element(ttmlns + "body")
                                                .Element(ttmlns + "div")
                                                .Elements(ttmlns + "p")
                                                .Select(snippet => snippet.Value));
        }

        public async Task AnalyseMediaAssets()
        {
            try
            {
                using (IAzureMediaServicesClient client = await CreateMediaServicesClientAsync())
                {
                    WriteLine("connected");

                    var assets = Directory.EnumerateFiles($"{mediaFolder}", "*.mp4", SearchOption.TopDirectoryOnly)
                                            .Where(f => !Directory.Exists($"{mediaOutputFolder}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(f)}"));

                    foreach (var asset in assets)
                    {
                        await IndexMediaAsset(client, asset);
                    }

                    WriteLine("Done.");
                    WriteLine("Press Enter to Continue");
                    ReadLine();

                }
            }
            catch (Exception exception)
            {
                if (exception.Source.Contains("ActiveDirectory"))
                {
                    Error.WriteLine("TIP: Make sure that you have filled out the appsettings.json file before running this sample.");
                }

                Error.WriteLine($"{exception.Message}");

                ApiErrorException apiException = exception.GetBaseException() as ApiErrorException;
                if (apiException != null)
                {
                    Error.WriteLine(
                        $"ERROR: API call failed with error code '{apiException.Body.Error.Code}' and message '{apiException.Body.Error.Message}'.");
                }


            }
        }

        private async Task IndexMediaAsset(IAzureMediaServicesClient client, string inputFile)
        {
            var outputFolder = $"{mediaOutputFolder}";

            // Set the polling interval for long running operations to 2 seconds.
            // The default value is 30 seconds for the .NET client SDK
            client.LongRunningOperationRetryTimeout = 2;

            // Creating a unique suffix so that we don't have name collisions if you run the sample
            // multiple times without cleaning up.
            string uniqueness = Guid.NewGuid().ToString("N");
            string jobName = $"job-{uniqueness}";
            string outputAssetName = Path.GetFileNameWithoutExtension(inputFile);
            string inputAssetName = $"input-{uniqueness}";

            // Ensure that you have the desired Transform. This is really a one time setup operation.
            //
            // In this Transform, we specify to use the VideoAnalyzerPreset preset. 
            // This preset enables you to extract multiple audio and video insights from a video. 
            // In the example, the language ("en-GB") is passed to its constructor. 
            // You can also specify what insights you want to extract by passing InsightsToExtract to the constructor.
            Transform videoAnalyzerTransform = await GetOrCreateTransformAsync(client, mediaServicesAuth.ResourceGroup, mediaServicesAuth.AccountName, "VideoAnalyzerTransformName", new VideoAnalyzerPreset("en-GB"));

            // Create a new input Asset and upload the specified local video file into it.
            await CreateInputAssetAsync(client, mediaServicesAuth.ResourceGroup, mediaServicesAuth.AccountName, inputAssetName, inputFile);

            // Use the name of the created input asset to create the job input.
            JobInput jobInput = new JobInputAsset(assetName: inputAssetName);

            // Output from the encoding Job must be written to an Asset, so let's create one
            Asset outputAsset = await CreateOutputAssetAsync(client, mediaServicesAuth.ResourceGroup, mediaServicesAuth.AccountName, outputAssetName);

            Job job = await SubmitJobAsync(client, mediaServicesAuth.ResourceGroup, mediaServicesAuth.AccountName, "VideoAnalyzerTransformName", jobName, jobInput, outputAsset.Name);

            // In this demo code, we will poll for Job status
            // Polling is not a recommended best practice for production applications because of the latency it introduces.
            // Overuse of this API may trigger throttling. Developers should instead use Event Grid.
            job = await WaitForJobToFinishAsync(client, mediaServicesAuth.ResourceGroup, mediaServicesAuth.AccountName, "VideoAnalyzerTransformName", jobName);

            if (job.State == JobState.Finished)
            {
                WriteLine("Job finished.");
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                await DownloadOutputAssetAsync(client, mediaServicesAuth.ResourceGroup, mediaServicesAuth.AccountName, outputAsset.Name, outputFolder);
            }
        }

        /// <summary>
        /// If the specified transform exists, get that transform.
        /// If the it does not exist, creates a new transform with the specified output. 
        /// In this case, the output is set to encode a video using one of the built-in encoding presets.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="transformName">The name of the transform.</param>
        /// <returns></returns>
        // <EnsureTransformExists>
        private async Task<Transform> GetOrCreateTransformAsync(IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string transformName,
            Preset preset)
        {
            // Does a Transform already exist with the desired name? Assume that an existing Transform with the desired name
            // also uses the same recipe or Preset for processing content.
            Transform transform = await client.Transforms.GetAsync(resourceGroupName, accountName, transformName);

            if (transform == null)
            {
                // Start by defining the desired outputs.
                TransformOutput[] outputs = new TransformOutput[]
                {
                    new TransformOutput(preset),
                };

                // Create the Transform with the output defined above
                transform = await client.Transforms.CreateOrUpdateAsync(resourceGroupName, accountName, transformName, outputs);
            }

            return transform;
        }
        // </EnsureTransformExists>

        /// <summary>
        /// Creates a new input Asset and uploads the specified local video file into it.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="assetName">The asset name.</param>
        /// <param name="fileToUpload">The file you want to upload into the asset.</param>
        /// <returns></returns>
        // <CreateInputAsset>
        private async Task<Asset> CreateInputAssetAsync(
            IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string assetName,
            string fileToUpload)
        {
            // In this example, we are assuming that the asset name is unique.
            //
            // If you already have an asset with the desired name, use the Assets.Get method
            // to get the existing asset. In Media Services v3, the Get method on entities returns null 
            // if the entity doesn't exist (a case-insensitive check on the name).

            // Call Media Services API to create an Asset.
            // This method creates a container in storage for the Asset.
            // The files (blobs) associated with the asset will be stored in this container.
            Asset asset = await client.Assets.CreateOrUpdateAsync(resourceGroupName, accountName, assetName, new Asset());

            // Use Media Services API to get back a response that contains
            // SAS URL for the Asset container into which to upload blobs.
            // That is where you would specify read-write permissions 
            // and the exparation time for the SAS URL.
            var response = await client.Assets.ListContainerSasAsync(
                resourceGroupName,
                accountName,
                assetName,
                permissions: AssetContainerPermission.ReadWrite,
                expiryTime: DateTime.UtcNow.AddHours(4).ToUniversalTime());

            var sasUri = new Uri(response.AssetContainerSasUrls.First());

            // Use Storage API to get a reference to the Asset container
            // that was created by calling Asset's CreateOrUpdate method.  
            CloudBlobContainer container = new CloudBlobContainer(sasUri);
            var blob = container.GetBlockBlobReference(Path.GetFileName(fileToUpload));

            // Use Strorage API to upload the file into the container in storage.
            await blob.UploadFromFileAsync(fileToUpload);

            return asset;
        }
        // </CreateInputAsset>

        /// <summary>
        /// Creates an ouput asset. The output from the encoding Job must be written to an Asset.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="assetName">The output asset name.</param>
        /// <returns></returns>
        // <CreateOutputAsset>
        private async Task<Asset> CreateOutputAssetAsync(IAzureMediaServicesClient client, string resourceGroupName, string accountName, string assetName)
        {
            // Check if an Asset already exists
            Asset outputAsset = await client.Assets.GetAsync(resourceGroupName, accountName, assetName);
            Asset asset = new Asset();
            string outputAssetName = assetName;

            if (outputAsset != null)
            {
                // Name collision! In order to get the sample to work, let's just go ahead and create a unique asset name
                // Note that the returned Asset can have a different name than the one specified as an input parameter.
                // You may want to update this part to throw an Exception instead, and handle name collisions differently.
                string uniqueness = $"-{Guid.NewGuid().ToString("N")}";
                outputAssetName += uniqueness;

                WriteLine("Warning – found an existing Asset with name = " + assetName);
                WriteLine("Creating an Asset with this name instead: " + outputAssetName);
            }

            return await client.Assets.CreateOrUpdateAsync(resourceGroupName, accountName, outputAssetName, asset);
        }
        // </CreateOutputAsset>

        /// <summary>
        /// Submits a request to Media Services to apply the specified Transform to a given input video.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="transformName">The name of the transform.</param>
        /// <param name="jobName">The (unique) name of the job.</param>
        /// <param name="jobInput"></param>
        /// <param name="outputAssetName">The (unique) name of the  output asset that will store the result of the encoding job. </param>
        // <SubmitJob>
        private async Task<Job> SubmitJobAsync(IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string transformName,
            string jobName,
            JobInput jobInput,
            string outputAssetName)
        {
            JobOutput[] jobOutputs =
            {
                new JobOutputAsset(outputAssetName),
            };

            // In this example, we are assuming that the job name is unique.
            //
            // If you already have a job with the desired name, use the Jobs.Get method
            // to get the existing job. In Media Services v3, Get methods on entities returns null 
            // if the entity doesn't exist (a case-insensitive check on the name).
            Job job = await client.Jobs.CreateAsync(
                resourceGroupName,
                accountName,
                transformName,
                jobName,
                new Job
                {
                    Input = jobInput,
                    Outputs = jobOutputs,
                });

            return job;
        }
        // </SubmitJob>

        /// <summary>
        /// Polls Media Services for the status of the Job.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="transformName">The name of the transform.</param>
        /// <param name="jobName">The name of the job you submitted.</param>
        /// <returns></returns>
        // <WaitForJobToFinish>
        private async Task<Job> WaitForJobToFinishAsync(IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string transformName,
            string jobName)
        {
            const int SleepIntervalMs = 60 * 1000;

            Job job = null;

            do
            {
                job = await client.Jobs.GetAsync(resourceGroupName, accountName, transformName, jobName);

                WriteLine($"Job is '{job.State}'.");
                for (int i = 0; i < job.Outputs.Count; i++)
                {
                    JobOutput output = job.Outputs[i];
                    Write($"\tJobOutput[{i}] is '{output.State}'.");
                    if (output.State == JobState.Processing)
                    {
                        Write($"  Progress: '{output.Progress}'.");
                    }

                    WriteLine();
                }

                if (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled)
                {
                    await Task.Delay(SleepIntervalMs);
                }
            }
            while (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled);

            return job;
        }
        // </WaitForJobToFinish>

        /// <summary>
        ///  Downloads the results from the specified output asset, so you can see what you got.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="assetName">The output asset.</param>
        /// <param name="outputFolderName">The name of the folder into which to download the results.</param>
        // <DownloadResults>
        private async Task DownloadOutputAssetAsync(
            IAzureMediaServicesClient client,
            string resourceGroup,
            string accountName,
            string assetName,
            string outputFolderName)
        {
            const int ListBlobsSegmentMaxResult = 5;

            if (!Directory.Exists(outputFolderName))
            {
                Directory.CreateDirectory(outputFolderName);
            }

            AssetContainerSas assetContainerSas = await client.Assets.ListContainerSasAsync(
                resourceGroup,
                accountName,
                assetName,
                permissions: AssetContainerPermission.Read,
                expiryTime: DateTime.UtcNow.AddHours(1).ToUniversalTime());

            Uri containerSasUrl = new Uri(assetContainerSas.AssetContainerSasUrls.FirstOrDefault());
            CloudBlobContainer container = new CloudBlobContainer(containerSasUrl);

            string directory = Path.Combine(outputFolderName, assetName);
            Directory.CreateDirectory(directory);

            WriteLine($"Downloading output results to '{directory}'...");

            BlobContinuationToken continuationToken = null;
            IList<Task> downloadTasks = new List<Task>();

            do
            {
                BlobResultSegment segment = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.None, ListBlobsSegmentMaxResult, continuationToken, null, null);

                foreach (IListBlobItem blobItem in segment.Results)
                {
                    CloudBlockBlob blob = blobItem as CloudBlockBlob;
                    if (blob != null)
                    {
                        string path = Path.Combine(directory, blob.Name);

                        downloadTasks.Add(blob.DownloadToFileAsync(path, FileMode.Create));
                    }
                }

                continuationToken = segment.ContinuationToken;
            }
            while (continuationToken != null);

            await Task.WhenAll(downloadTasks);

            WriteLine("Download complete.");
        }
        // </DownloadResults>

        /// <summary>
        /// Deletes the jobs and assets that were created.
        /// Generally, you should clean up everything except objects 
        /// that you are planning to reuse (typically, you will reuse Transforms, and you will persist StreamingLocators).
        /// </summary>
        /// <param name="client"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="accountName"></param>
        /// <param name="transformName"></param>
        // <CleanUp>
        private async Task CleanUpAsync(
            IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string transformName)
        {

            var jobs = await client.Jobs.ListAsync(resourceGroupName, accountName, transformName);
            foreach (var job in jobs)
            {
                await client.Jobs.DeleteAsync(resourceGroupName, accountName, transformName, job.Name);
            }

            var assets = await client.Assets.ListAsync(resourceGroupName, accountName);
            foreach (var asset in assets)
            {
                await client.Assets.DeleteAsync(resourceGroupName, accountName, asset.Name);
            }
        }
        // </CleanUp>

        private async Task<ServiceClientCredentials> GetCredentialsAsync()
        {
            // Use ApplicationTokenProvider.LoginSilentWithCertificateAsync or UserTokenProvider.LoginSilentAsync to get a token using service principal with certificate
            //// ClientAssertionCertificate
            //// ApplicationTokenProvider.LoginSilentWithCertificateAsync

            // Use ApplicationTokenProvider.LoginSilentAsync to get a token using a service principal with symetric key
            ClientCredential clientCredential = new ClientCredential(mediaServicesAuth.AadClientId, mediaServicesAuth.AadSecret);
            return await ApplicationTokenProvider.LoginSilentAsync(mediaServicesAuth.AadTenantId, clientCredential, ActiveDirectoryServiceSettings.Azure);
        }

        private async Task<IAzureMediaServicesClient> CreateMediaServicesClientAsync()
        {
            var credentials = await GetCredentialsAsync();

            return new AzureMediaServicesClient(credentials)
            {
                SubscriptionId = mediaServicesAuth.SubscriptionId,
            };
        }

    }
}