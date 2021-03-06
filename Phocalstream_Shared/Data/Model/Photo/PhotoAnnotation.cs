﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data.Model.Photo
{
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
