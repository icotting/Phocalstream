using Gif.Components;
using Microsoft.Practices.Unity;
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
        [ActionName("animate")]
        public HttpResponseMessage GenerateAnimation(string photoIds)
        {
            IEnumerable<long> photos = photoIds.Split(',').Select(s => Convert.ToInt64(s));
            
            string basePath = ConfigurationManager.AppSettings["photoPath"];

            AnimatedGifEncoder encoder = new AnimatedGifEncoder();
            encoder.Start(string.Format("{0}/videos/{1}.gif", basePath, Guid.NewGuid().ToString()));
            encoder.SetDelay(500);
            encoder.SetRepeat(0);

            foreach (long id in photos)
            {
                Photo photo = PhotoRepository.Single(p => p.ID == id, p => p.Site);
                encoder.AddFrame(Image.FromFile(string.Format("{0}/{1}/RAW/{2}.jpg", basePath, photo.Site.ContainerID, photo.BlobID)));
            }
            encoder.Finish();

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [HttpGet]
        [ActionName("raw")]
        public HttpResponseMessage GetPhoto(long id)
        {
            Photo photo = PhotoRepository.Single(p => p.ID == id, p => p.Site);
            if (photo == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            else
            {
                string basePath = ConfigurationManager.AppSettings["photoPath"];
                string photoPath = string.Format("{0}/{1}/RAW/{2}.jpg", basePath, photo.Site.ContainerID, photo.BlobID);

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
