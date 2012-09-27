using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models
{
    public class Photo
    {
        [Key]
        public long ID { get; set; }
        public string BlobID { get; set; }
        public DateTime Captured { get; set; }

        public ICollection<PhotoAnnotation> Annotations { get; set; }
        public CameraSite Site { get; set; }
    }
}