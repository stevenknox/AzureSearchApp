using Microsoft.Azure.Search;
using System;
using System.IO;

namespace AzureSearch
{
    public class SearchBase
    {
        internal string name = "wholesoftware";
        internal string apiKey = "";
        internal string index = "";
        internal readonly static string AzureCredentialsPath = $"{Environment.CurrentDirectory}/../.azure-credentials";
        internal SearchServiceClient CreateServiceClient() => new SearchServiceClient(name, new SearchCredentials(apiKey));
        internal SearchIndexClient CreateIndexClient() => new SearchIndexClient(name, index, new SearchCredentials(apiKey));

        public SearchBase()
        {
            apiKey = File.ReadAllText($"{AzureCredentialsPath}/search.private-azure-key");   
        }
    }
}