using System.IO;
using System.Net.Http;
using System.Text;
using AzureSearch;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [Route("api/media")]
    public class SearchController : Controller
    {
        private static string _baseFolder = $@"{Directory.GetCurrentDirectory()}\..\AzureSearch";

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
    }
}