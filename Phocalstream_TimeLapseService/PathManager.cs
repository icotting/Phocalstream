﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_TimeLapseService
{
    public static class PathManager
    {
        //Raw photo path
        private static string RawPath = ConfigurationManager.AppSettings["rawPath"];

        //Base path of Phocalstream directory
        private static string BasePath = ConfigurationManager.AppSettings["basePath"];

        //BasePath/Photo, BasePath/Timelapse
        private static string PhotoPath = ConfigurationManager.AppSettings["photoPath"];
        private static string TimelapsePath = ConfigurationManager.AppSettings["timelapsePath"];

        private static string MagickPath = ConfigurationManager.AppSettings["magickPath"];
        private static string ffmpegPath = ConfigurationManager.AppSettings["ffmpegPath"];

        private static string OutputPath = ConfigurationManager.AppSettings["outputPath"];

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

        public static string GetTimelapsePath()
        {
            return string.Format("{0}{1}", BasePath, TimelapsePath);
        }

        public static string GetMagickPath()
        {
            return string.Format("{0}{1}", BasePath, MagickPath);
        }

        public static string GetffmpegPath()
        {
            return string.Format("{0}{1}", BasePath, ffmpegPath);
        }

        public static string GetOutputPath()
        {
            return string.Format("{0}{1}", BasePath, OutputPath);
        }

        public static string GetDirectory(long Id)
        {
            return string.Format("{0}{1}{2}{3}", OutputPath, "/Job", Id, "/");
        }

        public static string GetDestination(long Id)
        {
            return string.Format("{0}{1}", GetDirectory(Id), "output.mpg");
        }
        public static string GetTempDirectory(long Id)
        {
            return string.Format("{0}{1}", GetDirectory(Id), "temp/");
        }

    }
}