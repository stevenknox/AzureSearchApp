using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace AzureSearch
{
    [SerializePropertyNamesAsCamelCase]
    public class AreaOfInterestIndexModel
    {
        [System.ComponentModel.DataAnnotations.Key]
        [IsFilterable]
        public string GlobalID { get; set; }
        [IsFilterable, IsSortable, IsSearchable]
        public string Name { get; set; }
        [IsSearchable]
        public string Address { get; set; }
        [IsFacetable]
        public string Postcode { get; set; }
        [IsFilterable, IsSortable, IsSearchable, IsFacetable]
        public string Town { get; set; }
        [IsFilterable, IsSortable, IsSearchable, IsFacetable]
        public string Facility { get; set; }
        [IsSearchable]
        public string Comments { get; set; }
        public Geometry Location { get; set; }

        public AreaOfInterestIndexModel()
        {

        }
    }

    public class AreasOfInterest
    {
        public string Type { get; set; }
        public List<Feature> Features { get; set; }

        public List<AreaOfInterestIndexModel> ToAreaOfInterestList()
        => Features.Select(s => new AreaOfInterestIndexModel
        {
            Name = s.Properties.Name,
            Address = s.Properties.Address,
            Postcode = s.Properties.Postcode,
            Town = s.Properties.Town,
            Facility = s.Properties.Facility,
            Comments = s.Properties.Comments,
            GlobalID = s.Properties.GlobalID,
            Location = s.Geometry
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