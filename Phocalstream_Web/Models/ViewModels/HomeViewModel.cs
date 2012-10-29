using Phocalstream_Shared.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class HomeViewModel
    {
        public ICollection<Collection> Collections { get; set; }
    }

    public class SiteDetails
    {
        public string SiteName { get; set; }

        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime First { get; set; }

        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime Last { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,#}")]
        public int PhotoCount { get; set; }
        public long SiteID { get; set; }

        public string LastPhotoURL { get; set; }
    }
}