using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models
{
    public class CameraSite
    {
        [Key]
        public long ID { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ContainerID { get; set; }

        [NotMapped]
        public int PhotoCount { get; set; }

        public ICollection<Photo> Photos { get; set; }
    }

    public class Collection
    {
        [Key]
        public long ID { get; set; }
        public string Name { get; set; }
        public virtual CameraSite Site { get; set; }
        public User Owner { get; set; }
        public ICollection<Photo> Photos { get; set; }

        // if the type is SITE then the Photos collection will be empty
        public CollectionType Type { get; set; }
        public CollectionStatus Status { get; set; }
    }

    public enum CollectionType
    {
        SITE,
        USER
    }

    public enum CollectionStatus
    {
        PROCESSING,
        COMPLETE
    }

    public class MetaDatum
    {
        public long ID { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public Photo Photo { get; set; }
    }

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

        public ICollection<MetaDatum> AdditionalExifProperties { get; set; }
        public ICollection<PhotoAnnotation> Annotations { get; set; }
        public ICollection<Collection> FoundIn { get; set; }
        public virtual CameraSite Site { get; set; }
    }

    public class PhotoAnnotation
    {
        [Key]
        public long ID { get; set; }
        public string Text { get; set; }
        public double Top { get; set; }
        public double Left { get; set; }
        public double Bottom { get; set; }
        public double Right { get; set; }
        public Photo Photo { get; set; }
        public DateTime Added { get; set; }
        public User Author { get; set; }
    }
}