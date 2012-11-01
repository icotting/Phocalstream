using Phocalstream_Shared.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class CollectionViewModel
    {
        public Collection Collection { get; set; }
        public string CollectionUrl { get; set; }
        public string SiteCoords { get; set; }
        public SiteDetails SiteDetails { get; set; }
    }
}
