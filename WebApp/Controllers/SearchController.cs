using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzureSearch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebApp.Hubs;

namespace WebApp.Controllers
{
    [Route("api/media")]
    [Route("api/search")]
    public class SearchController : Controller
    {
        private static readonly string _baseFolder = $@"{Directory.GetCurrentDirectory()}\..\AzureSearch";
        private readonly ISearchService _service;

         private readonly IHubContext<SearchHub> _hub;

        public SearchController(ISearchService service, IHubContext<SearchHub> hub)
        {
            _service = service;
            _hub = hub;
        }

        [HttpGet("{filename}.ttml")]
        public IActionResult TTML(string fileName)
        {
            var media = MediaSearch.Create().TTML(_baseFolder, fileName);

            return File(Encoding.UTF8.GetBytes(media), "application/ttml+xml");
        }

        [HttpGet("{filename}.vtt")]
        public IActionResult VTT(string fileName)
        {
            var media = MediaSearch.Create().VTT(_baseFolder, fileName);

            return File(Encoding.UTF8.GetBytes(media), "text/vtt");
        }

        [HttpGet("[action]/{filename}")]
        public IActionResult Transcript(string fileName)
        {
            var transcript = MediaSearch.Create().Transcript(_baseFolder, fileName);

            return Ok(transcript);
        }

        [HttpPost("[action]")]
        public IActionResult ObjectInfo([FromBody]CombinedSearch obj)
        {
            var info = SearchService.PrintObject(obj, outputAsHtml: true);

            return Ok(info);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UploadFile(IList<IFormFile> files)
        {
            foreach (IFormFile source in files)
            {
                string filename = ContentDispositionHeaderValue.Parse(source.ContentDisposition).FileName.Trim('"');

                filename = this.EnsureCorrectFilename(filename);

                using (MemoryStream output = new MemoryStream())
                {
                    source.CopyTo(output);
                    await FileSearch.Create().UploadFileToStorage(filename, output.ToArray());
                }
            }
            return Ok();            
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Reindex()
        {
            Action<string> handleEvents =  OnEventAdded;

            await _service.Reindex(_baseFolder, handleEvents); 

            return Ok();            
        }

        private void OnEventAdded(string @eve)
        {
            _hub.Clients.All.SendAsync("eventReceived", new object[] { @eve }).Wait();
        }

        private string EnsureCorrectFilename(string filename)
        {
            if (filename.Contains("\\"))
                filename = filename.Substring(filename.LastIndexOf("\\") + 1);

            return filename;
        }
    }
}