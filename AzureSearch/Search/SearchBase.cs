using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace AzureSearch
{
    public class SearchBase
    {
        internal string name = "wholesoftware";
        internal string apiKey = "";
        internal string storageKey = "";
        internal MediaServicesAuth mediaServicesAuth;
        internal string index = "";
        internal string basePath = "";
        internal SearchServiceClient CreateServiceClient() => new SearchServiceClient(name, new SearchCredentials(apiKey));
        internal SearchIndexClient CreateIndexClient() => new SearchIndexClient(name, index, new SearchCredentials(apiKey));
        internal Subject<string> _eventAdded = new Subject<string>();

        public IObservable<string> EventAdded
        {
            get { return _eventAdded.AsObservable(); }
        }

        public SearchBase()
        {
            index = "generic-index";
        }
    }
}