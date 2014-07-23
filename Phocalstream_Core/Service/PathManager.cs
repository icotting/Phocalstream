using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Phocalstream_Service.Service
{
    public static class PathManager
    {
        //Raw photo path
        public static string RawPath = ConfigurationManager.AppSettings["rawPath"];

        //Base path of Phocalstream directory
        public static string BasePath = ConfigurationManager.AppSettings["basePath"];
        
        //BasePath/Photo, BasePath/Search, BasePath/Download
        public static string PhotoPath = ConfigurationManager.AppSettings["photoPath"];
        public static string SearchPath = ConfigurationManager.AppSettings["searchPath"];
        public static string DownloadPath = ConfigurationManager.AppSettings["downloadPath"];


        //DB Connection Strings
        public static string DbConnection = ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString;

        public static string GetRawPath()
        {
            return RawPath;
        }

        public static string GetBasePath()
        {
            return BasePath;
        }

        public static string GetPhotoPath()
        {
            return string.Format("{0}{1}", BasePath, PhotoPath);
        }

        public static string GetSearchPath()
        {
            return string.Format("{0}{1}", BasePath, SearchPath);
        }

        public static string GetDownloadPath()
        {
            return string.Format("{0}{1}", BasePath, DownloadPath);
        }

        public static string GetDbConnection()
        {
            return DbConnection;
        }
    }
}