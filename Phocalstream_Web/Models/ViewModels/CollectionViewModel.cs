using Phocalstream_Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class CollectionViewModel
    {
        public Collection Collection { get; set; }
        public string CollectionUrl { get; set; }
        public string SiteCoords { get; set; }
        public int PhotoCount { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}
