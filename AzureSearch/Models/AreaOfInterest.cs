using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using static System.Guid;

namespace AzureSearch
{
    public class AreasOfInterest
    {
        public string Type { get; set; }
        public List<Feature> Features { get; set; }

         public List<SearchIndex> ToSearchIndex()
        => Features.Select(s => new SearchIndex
        {
            Id = NewGuid().ToString(),
            Name = s.Properties.Name,
            MergedText = JsonConvert.SerializeObject(s),
            Model = s.GetType().FullName,
            DataType = "Object"
        }).ToList();
    }
    public class Feature
    {
        public string Type { get; set; }
        public Properties Properties { get; set; }
        public Geometry Geometry { get; set; }
    }

    public class Properties
    {
        public int OBJECTID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Postcode { get; set; }
        public string Town { get; set; }
        public string Facility { get; set; }
        public string Comments { get; set; }
        public string GlobalID { get; set; }
    }

    public class Geometry
    {
        public string Type { get; set; }
        public List<double> Ccoordinates { get; set; }
    }
}