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

            if (resource.Contains("/dzc/"))
            {
                string blobRequest = string.Format("http://phocalstream.blob.core.windows.net/{0}",
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
                    WebClient client = new WebClient();
                    byte[] responseContent = client.DownloadData(blobRequest);

                    application.Context.Response.StatusCode = 200;
                    application.Context.Response.OutputStream.Write(responseContent, 0, responseContent.Length);
                }
                catch (WebException we)
                {
                    application.Context.Response.StatusCode = Convert.ToInt16(((HttpWebResponse)we.Response).StatusCode);
                }
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }

        public void Dispose()
        { }
    }
}