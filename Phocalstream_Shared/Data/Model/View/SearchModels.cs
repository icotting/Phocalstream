using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Phocalstream_Shared.Data.Model.View
{
    public class SearchModel
    {
        [Display(Name = "Sites")]
        public string Sites { get; set; }

        [Display(Name = "Season")]
        public string Seasons { get; set; }

        [Display(Name = "Months")]
        public string Months { get; set; }
        
        [Display(Name = "Dates")]
        public string Dates { get; set; }

        [Display(Name = "Tags")]
        public string Tags { get; set; }

        [Display(Name = "Times of Day")]
        public string TimesOfDay { get; set; }

        [Display(Name = "Hours of Day")]
        public string HoursOfDay { get; set; }

        

        public ICollection<string> SiteNames { get; set; }
        public ICollection<string> AvailableTags { get; set; }
    }

    public class SearchResults
    {
        public string Query { get; set; }
        public string CollectionUrl { get; set; }
    }

    public class SearchList
    {
        public List<Collection> Collections { get; set; }
        public string SearchPath { get; set; }
    }
}