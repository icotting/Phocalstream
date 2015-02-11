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
using Phocalstream_Shared.Data.Model.View;

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
        [ActionName("list")]
        public IEnumerable<Object> GetSites()
        {
            List<Object> details = new List<Object>();

            ICollection<Collection> collections = CollectionRepository.Find(c => c.Status == CollectionStatus.COMPLETE && c.Type == CollectionType.SITE, c => c.CoverPhoto, c => c.Site).ToList<Collection>();

            foreach ( Collection c in collections) 
            {
                details.Add(new { Details = GetDetailsForCollection(c), Latitude = c.Site.Latitude, Longitude = c.Site.Longitude });
            }

            return details;
        }

        private SiteDetails GetDetailsForCollection(Collection collection)
        {
            SiteDetails details = PhotoRepository.GetSiteDetails(collection.Site);
            details.CoverPhotoID = collection.CoverPhoto == null ? details.LastPhotoID : collection.CoverPhoto.ID;

            return details;
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
        public HttpResponseMessage PivotCollectionFor(int id, long year = -1)
        {
            // this should be moved into a service method
            Collection col = CollectionRepository.Single(c => c.ID == id, c => c.Site);

            XmlDocument doc = null;
            if (col.Type == CollectionType.SITE)
            {
                string rootDeepZoomPath = Path.Combine(PathManager.GetPhotoPath(), col.Site.DirectoryName);
                doc = new XmlDocument();
                if (year == -1)
                {
                    doc.Load(Path.Combine(rootDeepZoomPath, "site.cxml"));
                } 
                else
                {
                    doc.Load(Path.Combine(rootDeepZoomPath, string.Format("{0}_site.cxml", year)));
                }
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

            string email = UserRepository.First(u => u.ProviderID == this.User.Identity.Name).EmailAddress;
            List<string> downloadLinks = new List<string>();

            // For all groups of photosPerZip, save zip      
            int photosPerZip = 100;
            int photoCount = fileNames.Count();
            var fileCount = Math.Ceiling((Double)photoCount / (Double)photosPerZip);

            for (int i = 0; i < fileCount; i++)
            {
                string FileName = string.Format("{0}.{1}.zip", (DateTime.Now.ToString("MM-dd-yyyy-hh-mm-ss")), Convert.ToString(i + 1));

                string downloadURL = string.Format("{0}://{1}{2}",
                                                    Request.RequestUri.Scheme,
                                                    Request.RequestUri.Authority,
                                                    string.Format(@"/Photo/Download?fileName={0}", FileName));
                downloadLinks.Add(downloadURL);

                var startIndex = i * photosPerZip;
                var endIndex = (i + 1) * photosPerZip;

                List<string> subsetFileNames = new List<string>();
                if (photoCount > endIndex)
                {
                    subsetFileNames = fileNames.GetRange(startIndex, photosPerZip);
                }
                else
                {
                    var remainingCount = photoCount - startIndex;
                    subsetFileNames = fileNames.GetRange(startIndex, remainingCount);
                }

                DownloadImages(subsetFileNames, FileName);
            }

            var emailText = "";
            if (fileCount > 1)
            {
                emailText = string.Format("The images you requested for download were saved to {0} zip files. Please visit the links below to download the zip files. <br>{1}", Convert.ToString(fileCount), String.Join("<br>", downloadLinks));
            }
            else
            {
                emailText = "Please visit " + downloadLinks[0] + " to download the images.";
            }

            //after all saved, send email
            EmailService.SendMail(email, "Phocalstream Download", emailText);

        }

        private void DownloadImages(List<string> fileNames, string FileName)
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
        }
    }
}
