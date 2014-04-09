using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data.Model.Photo
{
    public class Tag
    {
        [Key]
        public long ID { get; set; }

        public string Name { get; set; }
        public ICollection<Photo> Photos { get; set; }
    }
}
