using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.Api
{
    public class UserSite
    {
        public long CollectionID { get; set; }
        public string Name { get; set; }
        public int PhotoCount { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public long CoverPhotoID { get; set; }
    }

    public class NewUserSiteModel
    {
        public string SiteName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string County { get; set; }
        public string State { get; set; }
    }

}