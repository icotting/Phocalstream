using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.Api
{
    public class DroughtMonitorRequest
    {
        public int CountyFips { get; set; }
        public string DmWeek { get; set; }
    }
}