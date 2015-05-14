using Gif.Components;
using Microsoft.Practices.Unity;
using Phocalstream_Service.Service;
using Phocalstream_Shared;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Service;
using Phocalstream_Web.Application;
using Phocalstream_Web.Application.Data;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;


namespace Phocalstream_Web.Controllers.Api
{
    public class PhotoController : ApiController
    {
        [Dependency]
        public IEntityRepository<Photo> PhotoRepository { get; set; }
        
        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IPhotoService PhotoService { get; set; }

        /* not sure if this is the right API controller for this (IC)
         * 
         * This method will download 1MB of data to the client and will be
         * used to determine the network speed of the client network. Based
         * on the determined speed, a cookie will be set to limit the image
         * sizes being returned by this controller. 
         */
        [HttpGet]
        [ActionName("speedtest")]
        public HttpResponseMessage TestConnectionSpeed()
        {
            byte[] data = new byte[1024];
            Random r = new Random();
            r.NextBytes(data);

            HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
            message.Content = new StreamContent(new MemoryStream(data));
            message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            return message;
        }

        [HttpGet]
        [ActionName("auto")]
        public HttpResponseMessage GetAutoResPhoto(long id)
        {
            CookieHeaderValue cookie = Request.Headers.GetCookies("image-size").FirstOrDefault();
            var res = cookie == null ? "low" : cookie["image-size"].Value;
            return loadPhoto(id, res);
        }

        [HttpGet]
        [ActionName("high")]
        public HttpResponseMessage GetHighResPhoto(long id)
        {
            return loadPhoto(id, "High");
        }

        [HttpGet]
        [ActionName("medium")]
        public HttpResponseMessage GetMidResPhoto(long id)
        {
            return loadPhoto(id, "Medium");
        }

        [HttpGet]
        [ActionName("low")]
        public HttpResponseMessage GetLowResPhoto(long id)
        {
            return loadPhoto(id, "Low");
        }

        private HttpResponseMessage loadPhoto(long id, string res)
        {
            Photo photo = PhotoRepository.Single(p => p.ID == id, p => p.Site);

            string photoPath = "";
            if (photo == null)
            {
                photoPath = HttpContext.Current.Server.MapPath("~/Content/Images/image_not_found.jpg");
            }
            else
            {
                Collection collection = CollectionRepository.Find(c => c.Site.ID == photo.Site.ID, c => c.Owner).FirstOrDefault();
                
                if (collection.Type == CollectionType.SITE)
                {
                    photoPath = string.Format("{0}/{1}/{2}.phocalstream/{3}.jpg", PathManager.GetPhotoPath(), photo.Site.DirectoryName, photo.BlobID, res);
                }
                else if (collection.Type == CollectionType.USER)
                {
                    photoPath = string.Format("{0}/{1}/{2}/{3}.phocalstream/{4}.jpg", PathManager.GetUserCollectionPath(), collection.Owner.ID, 
                        collection.ContainerID, photo.BlobID, res);
                }
                else 
                {
                    photoPath = HttpContext.Current.Server.MapPath("~/Content/Images/image_not_found.jpg");
                }
            }
            
            MemoryStream imageData = new MemoryStream();
            using (FileStream stream = File.OpenRead(photoPath))
            {
                int len = 0;
                byte[] buf = new byte[1024];
                while ((len = stream.Read(buf, 0, 1024)) > 0)
                {
                    imageData.Write(buf, 0, len);
                }
            }
            imageData.Position = 0;

            HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
            message.Content = new StreamContent(imageData);
            message.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

            return message;
            
        }
    }
}
