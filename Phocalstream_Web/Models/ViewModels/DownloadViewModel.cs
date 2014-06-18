using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class DownloadViewModel
    {
        public string DownloadPath { get; set; }
        public string[] Files { get; set; }
    }
}