using Microsoft.Practices.Unity;
using Phocalstream_Shared;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Web.Application;
using Phocalstream_Service.Data;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Xml;

namespace Phocalstream_Web.Controllers.Api
{
    public class SiteCollectionController : ApiController
    {
        [Dependency]
        public IPhotoRepository PhotoRepository { get; set; }

        [Dependency]
        public IEntityRepository<Photo> PhotoEntityRepository { get; set; }

        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IUnitOfWork UnitOfWork { get; set; }

        [HttpGet]
        [ActionName("updatecover")]
        public void SetCoverPhoto(long photoId, long collectionId)
        {
            Collection col = CollectionRepository.Single(c => c.ID == collectionId);
            Photo photo = PhotoEntityRepository.Single(p => p.ID == photoId);
            col.CoverPhoto = photo;
            CollectionRepository.Update(col);
            UnitOfWork.Commit();
        }

        [HttpGet]
        public HttpResponseMessage DeepZoomCollectionForSite(long siteID)
        {
                HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
                XmlDocument doc = PhotoRepository.CreateDeepZoomForSite(siteID);

                MemoryStream stream = new MemoryStream();
                doc.Save(stream);
                stream.Position = 0;
                message.Content = new StreamContent(stream);

                message.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");

                return message;
        }

        [HttpGet]
        public HttpResponseMessage DeepZoomCollection(string photoList)
        {
            HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
            XmlDocument doc = PhotoRepository.CreateDeepZomForList(photoList);
            MemoryStream stream = new MemoryStream();
            doc.Save(stream);
            stream.Position = 0;
            message.Content = new StreamContent(stream);

            message.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");

            return message;
        }
  
        [HttpGet, ActionName("PivotCollectionFor")]
        public HttpResponseMessage PivotCollectionFor(int id)
        {
            // this should be moved into a service method
            Collection col = CollectionRepository.Single(c => c.ID == id, c => c.Site);

            XmlDocument doc = null;
            if (col.Type == CollectionType.SITE)
            {
                string rootDeepZoomPath = Path.Combine(ConfigurationManager.AppSettings["PhotoPath"], col.Site.DirectoryName);
                doc = new XmlDocument();
                doc.Load(Path.Combine(rootDeepZoomPath, "site.cxml"));
            }
            else
            {
                doc = PhotoRepository.CreatePivotCollectionForSite(id);
            }

            HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
            MemoryStream stream = new MemoryStream();
            doc.Save(stream);
            stream.Position = 0;
            message.Content = new StreamContent(stream);

            message.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
            return message;
        }
    }
}
