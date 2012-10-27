using Phocalstream_Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class PhotoViewModel
    {
        public Photo Photo { get; set; }
        public string ImageUrl { get; set; }
        public string PhotoDate { get; set; }
        public string PhotoTime { get; set; }
        public string SiteCoords { get; set; }
    }
}