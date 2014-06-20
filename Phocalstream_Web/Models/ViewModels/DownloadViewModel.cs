using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class DownloadViewModel
    {
        public string DownloadPath { get; set; }
        
        //Tuple<file_name, size_string>
        public Tuple<string,string>[] Files { get; set; }
    }
}