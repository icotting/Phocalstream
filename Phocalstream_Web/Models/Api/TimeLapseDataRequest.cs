using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.Api
{
    public class TimeLapseDataRequest
    {
        public string IdList { get; set; }
        public int CountyFips { get; set; }
    }
}