using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data.Model.Photo
{
    public class Photo
    {
        [Key]
        public long ID { get; set; }
        public string BlobID { get; set; }
        public DateTime Captured { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public string UserComments { get; set; }
        public double ExposureTime { get; set; }
        public string ShutterSpeed { get; set; }
        public double MaxAperture { get; set; }
        public double FocalLength { get; set; }
        public bool Flash { get; set; }
        public int ISO { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string FileName { get; set; }

        public ICollection<Tag> Tags { get; set; }

        public ICollection<MetaDatum> AdditionalExifProperties { get; set; }
        public ICollection<PhotoAnnotation> Annotations { get; set; }
        public ICollection<Collection> FoundIn { get; set; }
        public virtual CameraSite Site { get; set; }
    }
}
