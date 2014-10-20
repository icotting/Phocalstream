using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.Api
{
    public class TimelapseDataRequest
    {
        public int CountyFips { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string DmWeek { get; set; }
    }
}