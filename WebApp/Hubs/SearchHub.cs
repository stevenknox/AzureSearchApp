using AzureSearch;
using Microsoft.AspNetCore.SignalR;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    public class SearchHub : Hub
    {
        public async Task Search(string term)
        {
           
        //    var textResults = TextSearch.Create()
        //                                 .Search(term)
        //                                 .ToCombinedSearch();

        //     var fileResults = FileSearch.Create()
        //                                 .Search(term)
        //                                 .ToCombinedSearch();

            // var mediaResults = MediaSearch.Create()
            //                               .Search(term)
            //                               .ToCombinedSearch();

            // var allResults = textResults.Concat(fileResults)
            //                             .Concat(mediaResults)
            //                             .ApplyIndex(startingIndex: 1)
            //                             .ToList();

            var allResults = MediaSearch.Create()
                                        .Search(term)
                                        .ToCombinedSearch()
                                        .ApplyIndex(startingIndex: 1)
                                        .ToList();

            await Clients.All.SendAsync("resultsReceived", allResults);
        }

        public async Task DownloadFile(string file, bool isImage)
        {
            FileSearch.Create().DownloadFile($@"{Directory.GetCurrentDirectory()}\..\AzureSearch\Downloads", file);

            await Clients.All.SendAsync("fileDownloaded", file, isImage);
        }
    }
}