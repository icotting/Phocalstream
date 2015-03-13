using Microsoft.Practices.Unity;
using Phocalstream_Shared;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Web.Application;
using Phocalstream_Web.Application.Data;
using Phocalstream_Web.Models;
using Phocalstream_Web.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Phocalstream_Shared.Data.Model.View;
using System.IO;
using Phocalstream_Service.Service;
using Phocalstream_Shared.Service;
using Phocalstream_Shared.Model.View;

namespace Phocalstream_Web.Controllers
{
    public class HomeController : Controller
    {
        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IEntityRepository<CameraSite> CameraSiteRepository { get; set; }

        [Dependency]
        public IPhotoRepository PhotoRepository { get; set; }

        [Dependency]
        public IPhotoService PhotoService { get; set; }

        [Dependency]
        public IEntityRepository<Photo> PhotoEntityRepository { get; set; }

        [Dependency]
        public IEntityRepository<Tag> TagRepository { get; set; }

        //
        // GET: /Home/
        public ActionResult Index(int e = 0)
        {
            HomeViewModel model = new HomeViewModel();
            ICollection<Collection> collections = CollectionRepository.Find(c => c.Status == CollectionStatus.COMPLETE && c.Type == CollectionType.SITE, c => c.CoverPhoto, c => c.Site).ToList<Collection>();

            model.Sites = collections.Select(c => GetDetailsForCollection(c)).ToArray();
            model.SiteThumbnails = new List<ThumbnailModel>();
            foreach (var s in model.Sites)
            {
                model.SiteThumbnails.Add(new ThumbnailModel()
                {
                    ID = s.SiteID,
                    Name = s.SiteName,
                    First = s.First,
                    Last = s.Last,
                    PhotoCount = s.PhotoCount,
                    CoverPhotoID = s.CoverPhotoID,
                    Link = "/photo/sitedashboard?siteId=" + s.SiteID.ToString()
                });
            }

            model.Tags = PhotoService.GetTagNames();

            model.SiteIndex = new Random().Next(model.Sites.Count());

            model.PublicCollectionThumbnails = new List<ThumbnailModel>();
            var publicCollections = CollectionRepository.Find(c => c.Type == CollectionType.USER && c.Public, c => c.Photos).ToList();
            foreach (var col in publicCollections)
            {
                if (col.CoverPhoto == null)
                {
                    col.CoverPhoto = col.Photos.LastOrDefault();
                }

                model.PublicCollectionThumbnails.Add(new ThumbnailModel()
                {
                    ID = col.ID,
                    Name = col.Name,
                    First = col.Photos.First().Captured,
                    Last = col.Photos.Last().Captured,
                    PhotoCount = col.Photos.Count,
                    CoverPhotoID = col.CoverPhoto.ID,
                    Link = "/search/index?collectionId=" + col.ID.ToString()
                });
            }

            if (e == 2)
            {
                ViewBag.Message = "Please enter at lease one search parameter.";
            }

            return View(model);
        }

        public ActionResult SiteList()
        {
            HomeViewModel model = new HomeViewModel();
            model.Collections = CollectionRepository.Find(c => c.Status == CollectionStatus.COMPLETE && c.Type == CollectionType.SITE, c => c.CoverPhoto, c => c.Site).ToList<Collection>();
            model.Sites = model.Collections.Select(c => GetDetailsForCollection(c)).ToArray();
            model.SiteThumbnails = new List<ThumbnailModel>();
            foreach (var s in model.Sites)
            {
                model.SiteThumbnails.Add(new ThumbnailModel()
                {
                    ID = s.SiteID,
                    Name = s.SiteName,
                    First = s.First,
                    Last = s.Last,
                    PhotoCount = s.PhotoCount,
                    CoverPhotoID = s.CoverPhotoID,
                    Link = "/photo/sitedashboard?siteId=" + s.SiteID.ToString()
                });
            }
            return View(model);
        }

        public ActionResult SiteDetails(long id)
        {
            Collection collection = CollectionRepository.First(c => c.Site.ID == id, c => c.Site, c => c.CoverPhoto);

            Phocalstream_Shared.Data.Model.View.SiteDetails details = PhotoRepository.GetSiteDetails(collection.Site);

            details.LastPhotoURL = string.Format("{0}://{1}:{2}/dzc/{3}/{4}.phocalstream/Tiles.dzi", Request.Url.Scheme,
                Request.Url.Host,
                Request.Url.Port,
                collection.Site.Name,
                collection.CoverPhoto == null ? PhotoEntityRepository.First(p => p.ID == details.LastPhotoID).BlobID : collection.CoverPhoto.BlobID);

            return PartialView("_SiteDetails", details);
        }

        public ActionResult TagList()
        {
            TagViewModel model = new TagViewModel();
            model.Tags = TagRepository.Find(t => !t.Name.Equals(""));
            model.TagThumbnails = model.Tags.Select(t => GetDetailsForTag(t));
            
            return View(model);
        }

        public ActionResult Downloads()
        {
            DownloadViewModel model = new DownloadViewModel();
            
            model.DownloadPath = PathManager.GetDownloadPath();
            FileInfo[] fileInfos = new DirectoryInfo(model.DownloadPath).GetFiles();

            Tuple<string, string>[] Files = new Tuple<string, string>[fileInfos.Length];

            for (int i = 0; i < Files.Length; i++ )
            {
                Files[i] = new Tuple<string, string>(fileInfos[i].Name, ToFileSize(fileInfos[i].Length));
            }

            model.Files = Files;

            return View(model);
        }

        private SiteDetails GetDetailsForCollection(Collection collection)
        {
            SiteDetails details = PhotoRepository.GetSiteDetails(collection.Site);
            details.CoverPhotoID = collection.CoverPhoto == null ? details.LastPhotoID : collection.CoverPhoto.ID;

            return details;
        }

        private ThumbnailModel GetDetailsForTag(Tag tag)
        {
            TagDetails details = PhotoRepository.GetTagDetails(tag);
            details.CoverPhotoID = details.LastPhotoID;

            return new ThumbnailModel()
                {
                    ID = details.TagID,
                    Name = details.TagName,
                    First = details.First,
                    Last = details.Last,
                    PhotoCount = details.PhotoCount,
                    CoverPhotoID = details.CoverPhotoID,
                    Link = "/search/index?tag=" + details.TagName
                };
        }

        //Utility method to convert FileSize to correct string
        private static string ToFileSize(long source)
        {
            const int byteConversion = 1024;
            double bytes = Convert.ToDouble(source);

            if (bytes >= Math.Pow(byteConversion, 3)) //GB Range
            {
                return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 3), 2), " GB");
            }
            else if (bytes >= Math.Pow(byteConversion, 2)) //MB Range
            {
                return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 2), 2), " MB");
            }
            else if (bytes >= byteConversion) //KB Range
            {
                return string.Concat(Math.Round(bytes / byteConversion, 2), " KB");
            }
            else //Bytes
            {
                return string.Concat(bytes, " Bytes");
            }
        }
    }
}
