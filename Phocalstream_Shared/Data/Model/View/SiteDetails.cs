using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data.Model.View
{
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

        public long CoverPhotoID { get; set; }

        // TODO: this should be removed in favor of using a cover photo element
        public long LastPhotoID { get; set; }
        public string LastPhotoURL { get; set; }
    }
}
