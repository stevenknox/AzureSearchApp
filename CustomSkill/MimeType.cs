using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureSearch.CustomSkill
{
    public class FriendlyMimeType
    {
        public string Extension { get; set; }
        public string Name { get; set; }
        public string Mime { get; set; }
    }
}
