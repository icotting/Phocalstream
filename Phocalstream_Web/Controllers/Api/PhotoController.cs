using Gif.Components;
using Microsoft.Practices.Unity;
using Phocalstream_Service.Service;
using Phocalstream_Shared;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
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
using System.Web.Http;


namespace Phocalstream_Web.Controllers.Api
{
    public class PhotoController : ApiController
    {
        [Dependency]
        public IEntityRepository<Photo> PhotoRepository { get; set; }

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
            if (photo == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            else
            {
                string basePath = PathManager.GetPhotoPath();
                string photoPath = string.Format("{0}/{1}/{2}.phocalstream/{3}.jpg", basePath, photo.Site.DirectoryName, photo.BlobID, res);

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
}
