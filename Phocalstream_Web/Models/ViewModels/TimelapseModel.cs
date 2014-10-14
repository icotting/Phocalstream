using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class TimelapseModel
    {
        public ICollection<TimeLapseFrame> Frames { get; set; }
        public string IDList { get; set; }
        public int CountyFips { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
    }
}