using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.Api
{
    public class UserSite
    {
        public string Name { get; set; }
        public int PhotoCount { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public long CoverPhotoID { get; set; }
    }
}