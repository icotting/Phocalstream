using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Phocalstream_Service.Service
{
    public static class PathManager
    {

        public static String GetBasePath()
        {
            return ConfigurationManager.AppSettings["basePath"];
        }

        public static String GetPhotoPath()
        {
            return ConfigurationManager.AppSettings["photoPath"];
        }


        public static String GetSearchPath()
        {
            return ConfigurationManager.AppSettings["searchPath"];
        }


        public static String GetRawPath()
        {
            return ConfigurationManager.AppSettings["rawPath"];
        }

        public static String GetDownloadPath()
        {
            return ConfigurationManager.AppSettings["downloadPath"];
        }


    }
}