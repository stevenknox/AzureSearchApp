using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AzureSearch.CustomSkill
{
    public static class FileInfo
    {
        private class WebApiResponseError
        {
            public string message { get; set; }
        }

        private class WebApiResponseWarning
        {
            public string message { get; set; }
        }

        private class WebApiResponseRecord
        {
            public string recordId { get; set; }
            public Dictionary<string, object> data { get; set; }
            public List<WebApiResponseError> errors { get; set; }
            public List<WebApiResponseWarning> warnings { get; set; }
        }

        private class WebApiEnricherResponse
        {
            public List<WebApiResponseRecord> values { get; set; }
        }

        [FunctionName("FileInfo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string recordId = null;
            string contentType = null;

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            recordId = data?.values?.First?.recordId?.Value as string;
            contentType = data?.values?.First?.contentType?.Value as string;

            if (recordId == null)
            {
                return new BadRequestObjectResult("recordId cannot be null");
            }

            var fileDescription = MimeTypes.FirstOrDefault(f => f.Mime == contentType);

            // Put together response.
            WebApiResponseRecord responseRecord = new WebApiResponseRecord
            {
                data = new Dictionary<string, object>(),
                recordId = recordId
            };
            responseRecord.data.Add("dataType", fileDescription == null ? "Unkown File Type" : fileDescription.Name);

            WebApiEnricherResponse response = new WebApiEnricherResponse();
            response.values = new List<WebApiResponseRecord>();
            response.values.Add(responseRecord);

            return (ActionResult)new OkObjectResult(response);
        }

        private static List<FriendlyMimeType> MimeTypes = JsonConvert.DeserializeObject<List<FriendlyMimeType>>(@"
[
    {
      ""Extension"": "".aac"",
      ""Name"": ""AAC audio"",
      ""Mime"": ""audio/aac""
    },
    {
      ""Extension"": "".abw"",
      ""Name"": ""AbiWord document"",
      ""Mime"": ""application/x-abiword""
    },
    {
      ""Extension"": "".arc"",
      ""Name"": ""Archive document (multiple files embedded)"",
      ""Mime"": ""application/x-freearc""
    },
    {
      ""Extension"": "".avi"",
      ""Name"": ""AVI: Audio Video Interleave"",
      ""Mime"": ""video/x-msvideo""
    },
    {
      ""Extension"": "".azw"",
      ""Name"": ""Amazon Kindle eBook format"",
      ""Mime"": ""application/vnd.amazon.ebook""
    },
    {
      ""Extension"": "".bin"",
      ""Name"": ""Any kind of binary data"",
      ""Mime"": ""application/octet-stream""
    },
    {
      ""Extension"": "".bmp"",
      ""Name"": ""Windows OS/2 Bitmap Graphics"",
      ""Mime"": ""image/bmp""
    },
    {
      ""Extension"": "".bz"",
      ""Name"": ""BZip archive"",
      ""Mime"": ""application/x-bzip""
    },
    {
      ""Extension"": "".bz2"",
      ""Name"": ""BZip2 archive"",
      ""Mime"": ""application/x-bzip2""
    },
    {
      ""Extension"": "".csh"",
      ""Name"": ""C-Shell script"",
      ""Mime"": ""application/x-csh""
    },
    {
      ""Extension"": "".css"",
      ""Name"": ""Cascading Style Sheets (CSS)"",
      ""Mime"": ""text/css""
    },
    {
      ""Extension"": "".csv"",
      ""Name"": ""Comma-separated values (CSV)"",
      ""Mime"": ""text/csv""
    },
    {
      ""Extension"": "".doc"",
      ""Name"": ""Microsoft Word"",
      ""Mime"": ""application/msword""
    },
    {
      ""Extension"": "".docx"",
      ""Name"": ""Microsoft Word (OpenXML)"",
      ""Mime"": ""application/vnd.openxmlformats-officedocument.wordprocessingml.document""
    },
    {
      ""Extension"": "".eot"",
      ""Name"": ""MS Embedded OpenType fonts"",
      ""Mime"": ""application/vnd.ms-fontobject""
    },
    {
      ""Extension"": "".epub"",
      ""Name"": ""Electronic publication (EPUB)"",
      ""Mime"": ""application/epub+zip""
    },
    {
      ""Extension"": "".gif"",
      ""Name"": ""Graphics Interchange Format (GIF)"",
      ""Mime"": ""image/gif""
    },
    {
      ""Extension"": "".htm"",
      ""Name"": ""HyperText Markup Language (HTML)"",
      ""Mime"": ""text/html""
    },
    {
      ""Extension"": "".html"",
      ""Name"": """",
      ""Mime"": """"
    },
    {
      ""Extension"": "".ico"",
      ""Name"": ""Icon format"",
      ""Mime"": ""image/vnd.microsoft.icon""
    },
    {
      ""Extension"": "".ics"",
      ""Name"": ""iCalendar format"",
      ""Mime"": ""text/calendar""
    },
    {
      ""Extension"": "".jar"",
      ""Name"": ""Java Archive (JAR)"",
      ""Mime"": ""application/java-archive""
    },
    {
      ""Extension"": "".jpeg"",
      ""Name"": ""JPEG images"",
      ""Mime"": ""image/jpeg""
    },
    {
      ""Extension"": "".jpg"",
      ""Name"": """",
      ""Mime"": """"
    },
    {
      ""Extension"": "".js"",
      ""Name"": ""JavaScript"",
      ""Mime"": ""text/javascript""
    },
    {
      ""Extension"": "".json"",
      ""Name"": ""JSON format"",
      ""Mime"": ""application/json""
    },
    {
      ""Extension"": "".jsonld"",
      ""Name"": ""JSON-LD format"",
      ""Mime"": ""application/ld+json""
    },
    {
      ""Extension"": "".mid"",
      ""Name"": ""Musical Instrument Digital Interface (MIDI)"",
      ""Mime"": ""audio/midi""
    },
    {
      ""Extension"": "".mjs"",
      ""Name"": ""JavaScript module"",
      ""Mime"": ""text/javascript""
    },
    {
      ""Extension"": "".mp3"",
      ""Name"": ""MP3 audio"",
      ""Mime"": ""audio/mpeg""
    },
    {
      ""Extension"": "".mpeg"",
      ""Name"": ""MPEG Video"",
      ""Mime"": ""video/mpeg""
    },
    {
      ""Extension"": "".mpkg"",
      ""Name"": ""Apple Installer Package"",
      ""Mime"": ""application/vnd.apple.installer+xml""
    },
    {
      ""Extension"": "".odp"",
      ""Name"": ""OpenDocument presentation document"",
      ""Mime"": ""application/vnd.oasis.opendocument.presentation""
    },
    {
      ""Extension"": "".ods"",
      ""Name"": ""OpenDocument spreadsheet document"",
      ""Mime"": ""application/vnd.oasis.opendocument.spreadsheet""
    },
    {
      ""Extension"": "".odt"",
      ""Name"": ""OpenDocument text document"",
      ""Mime"": ""application/vnd.oasis.opendocument.text""
    },
    {
      ""Extension"": "".oga"",
      ""Name"": ""OGG audio"",
      ""Mime"": ""audio/ogg""
    },
    {
      ""Extension"": "".ogv"",
      ""Name"": ""OGG video"",
      ""Mime"": ""video/ogg""
    },
    {
      ""Extension"": "".ogx"",
      ""Name"": ""OGG"",
      ""Mime"": ""application/ogg""
    },
    {
      ""Extension"": "".otf"",
      ""Name"": ""OpenType font"",
      ""Mime"": ""font/otf""
    },
    {
      ""Extension"": "".png"",
      ""Name"": ""Portable Network Graphics"",
      ""Mime"": ""image/png""
    },
    {
      ""Extension"": "".pdf"",
      ""Name"": ""Adobe Portable Document Format (PDF)"",
      ""Mime"": ""application/pdf""
    },
    {
      ""Extension"": "".ppt"",
      ""Name"": ""Microsoft PowerPoint"",
      ""Mime"": ""application/vnd.ms-powerpoint""
    },
    {
      ""Extension"": "".pptx"",
      ""Name"": ""Microsoft PowerPoint (OpenXML)"",
      ""Mime"": ""application/vnd.openxmlformats-officedocument.presentationml.presentation""
    },
    {
      ""Extension"": "".rar"",
      ""Name"": ""RAR archive"",
      ""Mime"": ""application/x-rar-compressed""
    },
    {
      ""Extension"": "".rtf"",
      ""Name"": ""Rich Text Format (RTF)"",
      ""Mime"": ""application/rtf""
    },
    {
      ""Extension"": "".sh"",
      ""Name"": ""Bourne shell script"",
      ""Mime"": ""application/x-sh""
    },
    {
      ""Extension"": "".svg"",
      ""Name"": ""Scalable Vector Graphics (SVG)"",
      ""Mime"": ""image/svg+xml""
    },
    {
      ""Extension"": "".swf"",
      ""Name"": ""Small web format (SWF) or Adobe Flash document"",
      ""Mime"": ""application/x-shockwave-flash""
    },
    {
      ""Extension"": "".tar"",
      ""Name"": ""Tape Archive (TAR)"",
      ""Mime"": ""application/x-tar""
    },
    {
      ""Extension"": "".tif"",
      ""Name"": ""Tagged Image File Format (TIFF)"",
      ""Mime"": ""image/tiff""
    },
    {
      ""Extension"": "".tiff"",
      ""Name"": """",
      ""Mime"": """"
    },
    {
      ""Extension"": "".ts"",
      ""Name"": ""MPEG transport stream"",
      ""Mime"": ""video/mp2t""
    },
    {
      ""Extension"": "".ttf"",
      ""Name"": ""TrueType Font"",
      ""Mime"": ""font/ttf""
    },
    {
      ""Extension"": "".txt"",
      ""Name"": ""Text, (generally ASCII or ISO 8859-n)"",
      ""Mime"": ""text/plain""
    },
    {
      ""Extension"": "".vsd"",
      ""Name"": ""Microsoft Visio"",
      ""Mime"": ""application/vnd.visio""
    },
    {
      ""Extension"": "".wav"",
      ""Name"": ""Waveform Audio Format"",
      ""Mime"": ""audio/wav""
    },
    {
      ""Extension"": "".weba"",
      ""Name"": ""WEBM audio"",
      ""Mime"": ""audio/webm""
    },
    {
      ""Extension"": "".webm"",
      ""Name"": ""WEBM video"",
      ""Mime"": ""video/webm""
    },
    {
      ""Extension"": "".webp"",
      ""Name"": ""WEBP image"",
      ""Mime"": ""image/webp""
    },
    {
      ""Extension"": "".woff"",
      ""Name"": ""Web Open Font Format (WOFF)"",
      ""Mime"": ""font/woff""
    },
    {
      ""Extension"": "".woff2"",
      ""Name"": ""Web Open Font Format (WOFF)"",
      ""Mime"": ""font/woff2""
    },
    {
      ""Extension"": "".xhtml"",
      ""Name"": ""XHTML"",
      ""Mime"": ""application/xhtml+xml""
    },
    {
      ""Extension"": "".xls"",
      ""Name"": ""Microsoft Excel"",
      ""Mime"": ""application/vnd.ms-excel""
    },
    {
      ""Extension"": "".xlsx"",
      ""Name"": ""Microsoft Excel (OpenXML)"",
      ""Mime"": ""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet""
    },
    {
      ""Extension"": "".xml"",
      ""Name"": ""XML"",
      ""Mime"": ""application/xml""
    },
    {
      ""Extension"": """",
      ""Name"": """",
      ""Mime"": ""text/xml""
    },
    {
      ""Extension"": "".xul"",
      ""Name"": ""XUL"",
      ""Mime"": ""application/vnd.mozilla.xul+xml""
    },
    {
      ""Extension"": "".zip"",
      ""Name"": ""ZIP archive"",
      ""Mime"": ""application/zip""
    },
    {
      ""Extension"": "".3gp"",
      ""Name"": ""3GPP audio/video container"",
      ""Mime"": ""video/3gpp""
    },
    {
      ""Extension"": "".3g2"",
      ""Name"": ""3GPP2 audio/video container"",
      ""Mime"": ""video/3gpp2""
    },
    {
      ""Extension"": "".7z"",
      ""Name"": ""7-zip archive"",
      ""Mime"": ""application/x-7z-compressed""
    }
   ]");
    }
}
