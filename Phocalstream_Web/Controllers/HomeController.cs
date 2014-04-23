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
        public IEntityRepository<Photo> PhotoEntityRepository { get; set; }

        //
        // GET: /Home/
        public ActionResult Index()
        {
            HomeViewModel model = new HomeViewModel();
            ICollection<Collection> collections = CollectionRepository.Find(c => c.Status == CollectionStatus.COMPLETE && c.Type == CollectionType.SITE, c => c.CoverPhoto, c => c.Site).ToList<Collection>();
            model.Sites = collections.Select(c => GetDetailsForCollection(c));

            return View(model);
        }

        public ActionResult SiteList()
        {
            HomeViewModel model = new HomeViewModel();
            model.Collections = CollectionRepository.Find(c => c.Status == CollectionStatus.COMPLETE && c.Type == CollectionType.SITE, c => c.CoverPhoto, c => c.Site).ToList<Collection>();
            model.Sites = model.Collections.Select(c => GetDetailsForCollection(c));
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

        private SiteDetails GetDetailsForCollection(Collection collection)
        {
            SiteDetails details = PhotoRepository.GetSiteDetails(collection.Site);
            details.CoverPhotoID = collection.CoverPhoto == null ? details.LastPhotoID : collection.CoverPhoto.ID;

            return details;
        }
    }
}
