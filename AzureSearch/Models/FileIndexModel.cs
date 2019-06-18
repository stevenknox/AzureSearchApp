namespace AzureSearch
{
    public class FileIndexModel
    {
        public string metadata_storage_name { get; set; }
        public string metadata_content_type { get; set; }
        public string[] keyphrases { get; set; }
    }
}