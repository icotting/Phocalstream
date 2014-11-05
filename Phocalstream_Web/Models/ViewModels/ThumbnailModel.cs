using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class ThumbnailModel
    {
        public long ID { get; set; }

        public string Name { get; set; }
        
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime First { get; set; }

        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime Last { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,#}")]
        public int PhotoCount { get; set; }

        public long CoverPhotoID { get; set; }

        public string Link { get; set; }
   }
}