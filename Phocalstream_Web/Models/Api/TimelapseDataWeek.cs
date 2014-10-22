using Phocalstream_Shared.Data.Model.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.Api
{
    public class TimelapseDataWeek
    {
        public IDictionary<string, DroughtMonitorWeek>  DMData { get; set; }
        public double AverageDischarge { get; set; }
    }
}