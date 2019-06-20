using System.IO;
using AzureSearch;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;


public interface IAzureCredentials
{
    string ApiKey { get; set; }
    string StorageKey { get; set; }
    MediaServicesAuth MediaServicesAuth { get; set; }
}

public class AzureCredentials : IAzureCredentials
{
    public string ApiKey { get; set; }
    public string StorageKey { get; set; }
    public MediaServicesAuth MediaServicesAuth { get; set; }
    public AzureCredentials(IHostingEnvironment env)
    {
        var AzureCredentialsPath = $"{env.ContentRootPath}/../.azure-credentials";
        
        ApiKey = File.ReadAllText($"{AzureCredentialsPath}/search.private-azure-key"); 
        MediaServicesAuth =  JsonConvert.DeserializeObject<MediaServicesAuth>(File.ReadAllText($"{AzureCredentialsPath}/media.private-azure-key")); 
        StorageKey = File.ReadAllText($"{AzureCredentialsPath}/storage.private-azure-key");
    }
}
