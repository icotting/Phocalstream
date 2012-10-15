using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.Entity
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
        public int PhotoCount { get { return Photos == null ? 0 : Photos.Count; } }

        public ICollection<Photo> Photos { get; set; }
    }
}