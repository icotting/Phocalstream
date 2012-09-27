using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        public ICollection<Photo> Photos { get; set; }
    }
}