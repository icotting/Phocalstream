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
        public IEnumerable<DateTime> DmWeeks { get; set; }
        public int CountyFips { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

    }
}