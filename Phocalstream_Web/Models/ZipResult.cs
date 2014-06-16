using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Phocalstream_Web.Models
{
    public class ZipResult : ActionResult
    {
        private static string _path;
        private IEnumerable<string> _files;
        private string _fileName;

        public string FileName
        {
            get
            {
                return _fileName ?? (DateTime.Now.ToString("MM-dd-yyyy-h-mm") + ".zip");
            }
            set { _fileName = value; }
        }

        public ZipResult(params string[] files)
        {
            this._files = files;
            _path = ConfigurationManager.AppSettings["rawPath"];
        }

        public ZipResult(IEnumerable<string> files)
        {
            this._files = files;
            _path = ConfigurationManager.AppSettings["rawPath"];
        }

        public override void ExecuteResult(ControllerContext context)
        {
            
            var closer = new Ionic.Zip.CloseDelegate((name, stream) =>
            {
                stream.Dispose();
            });

            using (ZipFile zf = new ZipFile())
            {
                foreach (var file in _files)
                {
                    var getOpener = new Ionic.Zip.OpenDelegate(name =>
                    {
                        WebClient c = new WebClient();
                        return c.OpenRead(Path.Combine(_path, file));
                    });

                    zf.AddEntry(file, getOpener, closer);
                }

                //context.HttpContext.Response.ContentType = "application/zip";
                //context.HttpContext.Response.AppendHeader("content-disposition", "attachment; filename=" + FileName);
             
                zf.Save(Path.Combine("C:/Users/Zach/Desktop", FileName));
            }
        }

    }
}
