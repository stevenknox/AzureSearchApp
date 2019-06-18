using AzureSearch;
using Microsoft.AspNetCore.SignalR;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    public class SearchHub : Hub
    {
        private readonly ISearchService _service;

        public SearchHub(ISearchService service)
        {
            _service = service;
        }
        public async Task Search(string term)
        {
           var results = _service.Search(term)
                .ToCombinedSearch()
                .ApplyIndex(startingIndex: 1)
                .ToList();


            await Clients.All.SendAsync("resultsReceived", results);
        }

        public async Task DownloadFile(string file, bool isImage)
        {
            FileSearch.Create().DownloadFile($@"{Directory.GetCurrentDirectory()}\..\AzureSearch\Downloads", file);

            await Clients.All.SendAsync("fileDownloaded", file, isImage);
        }
    }
}