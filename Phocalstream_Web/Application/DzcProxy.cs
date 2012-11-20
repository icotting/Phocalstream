using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace Phocalstream_Web.Application
{
    public class DzcProxy : IHttpModule
    {
        public void Init(HttpApplication application)
        {
            application.BeginRequest += (new EventHandler(this.Application_BeginRequest));
        }

        private void Application_BeginRequest(Object source, EventArgs e)
        {
            HttpApplication application = (HttpApplication)source;
            HttpContext context = application.Context;
            string resource = application.Request.RawUrl;
            System.Diagnostics.Debug.WriteLine(resource);
            if (resource.Contains("/dzc/"))
            {
                Console.WriteLine(resource);
                string blobRequest = string.Format(@"C:/Photos/{0}",
                        resource.Substring(resource.IndexOf("/dzc/") + 5));

                string extension = (resource.IndexOf(".") > -1) ? resource.Substring(resource.LastIndexOf(".")) : "";
                switch (extension)
                {
                    case ".dzc":
                    case ".dzi":
                        application.Context.Response.AddHeader("Content-Type", "text/xml");
                        break;
                    case ".jpg":
                        application.Context.Response.AddHeader("Content-Type", "image/jpeg");
                        break;
                }

                try
                {
                    using (FileStream stream = File.OpenRead(blobRequest))
                    {
                        int len = 0;
                        byte[] buf = new byte[1024];
                        while ((len = stream.Read(buf, 0, 1024)) > 0)
                        {
                            application.Context.Response.OutputStream.Write(buf, 0, len);
                        }
                    }
                    application.Context.Response.StatusCode = 200;
                }
                catch (DirectoryNotFoundException we)
                {
                    application.Context.Response.StatusCode = 404;
                }
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }

        public void Dispose()
        { }
    }
}