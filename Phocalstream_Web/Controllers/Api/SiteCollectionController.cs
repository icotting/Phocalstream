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
using Phocalstream_Service.Service;
using Ionic.Zip;
using System.Diagnostics;
using Phocalstream_Shared.Service;

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
        public IUnitOfWork Unit { get; set; }

        [Dependency]
        public IEntityRepository<User> UserRepository { get; set; }

        [Dependency]
        public IPhotoService PhotoService { get; set; }

        [Dependency]
        public ICollectionService CollectionService { get; set; }


        [HttpGet]
        [ActionName("updatecover")]
        public void SetCoverPhoto(long photoId, long collectionId)
        {
            Collection col = CollectionRepository.Single(c => c.ID == collectionId);
            Photo photo = PhotoEntityRepository.Single(p => p.ID == photoId);
            col.CoverPhoto = photo;
            CollectionRepository.Update(col);
            Unit.Commit();
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
                string rootDeepZoomPath = Path.Combine(PathManager.GetPhotoPath(), col.Site.DirectoryName);
                doc = new XmlDocument();
                doc.Load(Path.Combine(rootDeepZoomPath, "site.cxml"));
            }
            else if (col.Type == CollectionType.SEARCH)
            {
                string rootDeepZoomPath = Path.Combine(PathManager.GetSearchPath(), col.ContainerID);
                doc = new XmlDocument();
                doc.Load(Path.Combine(rootDeepZoomPath, "site.cxml"));
            }
            else if (col.Type == CollectionType.USER)
            {
                string rootDeepZoomPath = Path.Combine(PathManager.GetUserCollectionPath(), col.ContainerID);
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

        [HttpPost, ActionName("RawDownload")]
        public void FullResolutionDownload(string photoIds)
        {
            //need to access photos and names on main thread
            List<string> fileNames = new List<string>();

            string[] ids = photoIds.Split(',');

            foreach (var id in ids)
            {
                long photoID = Convert.ToInt32(id);
                Photo photo = PhotoEntityRepository.Single(p => p.ID == photoID, p => p.Site);

                if (photo != null)
                {
                    fileNames.Add(photo.FileName);
                }
            }

            string email = UserRepository.First(u => u.GoogleID == this.User.Identity.Name).EmailAddress;

            string FileName = (DateTime.Now.ToString("MM-dd-yyyy-h-mm") + ".zip");

            string downloadURL = string.Format("{0}://{1}{2}",
                                                Request.RequestUri.Scheme,
                                                Request.RequestUri.Authority,
                                                string.Format(@"/Photo/Download?fileName={0}", FileName));

            DownloadImages(fileNames, FileName, email, downloadURL);
        }

        private void DownloadImages(List<string> fileNames, string FileName, string email, string downloadURL)
        {
            string path = PathManager.GetRawPath();

            if (String.IsNullOrEmpty(path))
            {
                throw new Exception("The raw photo path is null or empty");
            }

            string save_path = PathManager.GetDownloadPath();
            if (!Directory.Exists(save_path))
            {
                Directory.CreateDirectory(save_path);
            }

            //closer for save process
            var closer = new Ionic.Zip.CloseDelegate((name, stream) =>
            {
                stream.Dispose();
            });

            using (ZipFile zf = new ZipFile())
            {
                foreach (var file in fileNames)
                {
                    string fullName = file;
                    /* for some reason path combine does not work on the server this needs to be further investigated */
                    if (!fullName.StartsWith(@"\"))
                    {
                       fullName = string.Format(@"{0}\{1}", path, file);
                    }
                    else
                    {
                        fullName = string.Format(@"{0}{1}", path, file);
                    }

                    if (!File.Exists(fullName))
                    {
                        throw new Exception(string.Format(@"The file {0} does not exist", fullName));
                    }

                    var getOpener = new Ionic.Zip.OpenDelegate(name =>
                    {
                        WebClient c = new WebClient();
                        return c.OpenRead(fullName);
                    });

                    zf.AddEntry(Path.GetFileName(file), getOpener, closer);
                }

                zf.Save(Path.Combine(save_path, FileName));
            }

            //after save, send email
            EmailService.SendMail(email, "Phocalstream Download", "Please visit " + downloadURL + " to download the images.");
        }
    
        [HttpPost, ActionName("SaveUserCollection")]
        public void SaveUserCollection(string collectionName, string photoIds)
        {
            long[] ids = photoIds.Split(',').Select(i => Convert.ToInt64(i)).ToArray();
            List<Photo> photos = PhotoEntityRepository.Find(p => ids.Contains(p.ID)).ToList();

            Guid containerID = Guid.NewGuid();

            //save the collection
            Collection c = new Collection();
            c.Name = collectionName;
            c.ContainerID = containerID.ToString();
            c.Owner = UserRepository.First(u => u.GoogleID == this.User.Identity.Name);
            c.Type = CollectionType.USER;
            c.Photos = photos;
            CollectionRepository.Insert(c);
            Unit.Commit();

            //generate xml manifests
            string collectionPath = CollectionService.ValidateAndGetUserCollectionPath();
            CollectionService.GenerateCollectionManifest(PhotoService.GetFileNames(photos),
                Path.Combine(collectionPath, containerID.ToString(), "collection.dzc"));
            PhotoService.GeneratePivotManifest(collectionPath, containerID.ToString(), String.Join(",", ids), CollectionType.USER);
        }
    }
}
