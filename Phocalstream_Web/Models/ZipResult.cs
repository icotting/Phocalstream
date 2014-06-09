using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Phocalstream_Web.Models
{
    public class ZipResult : ActionResult
    {
        private IEnumerable<string> _files;
        private string _fileName;

        public string FileName
        {
            get
            {
                return _fileName ?? "file.zip";
            }
            set { _fileName = value; }
        }

        public ZipResult(params string[] files)
        {
            this._files = files;
        }

        public ZipResult(IEnumerable<string> files)
        {
            this._files = files;
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
                        return c.OpenRead(file);
                    });

                    zf.AddEntry(file, getOpener, closer);
                }

                context.HttpContext.Response.ContentType = "application/zip";
                context.HttpContext.Response.AppendHeader("content-disposition", "attachment; filename=" + FileName);
             
                zf.Save(context.HttpContext.Response.OutputStream);
            }
        }

        

    }
}
