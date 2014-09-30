using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Phocalstream_Shared.Data.Model.Photo
{
    public class CameraSite
    {
        [Key]
        public long ID { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int CountyFips { get; set; }
        public string ContainerID { get; set; }
        public string DirectoryName { get; set; }
        [NotMapped]
        public int PhotoCount { get; set; }

        [ScriptIgnore]
        public ICollection<Photo> Photos { get; set; }
    }
}
