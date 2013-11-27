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
            model.Collections = CollectionRepository.AsQueryable().Where(c => c.Status == CollectionStatus.COMPLETE && c.Type == CollectionType.SITE).ToList<Collection>();
            return View(model);
        }

        public ActionResult SiteDetails(long id)
        {
            CameraSite site = CameraSiteRepository.First(s => s.ID == id);
            Phocalstream_Shared.Data.Model.View.SiteDetails details = PhotoRepository.GetSiteDetails(site);

            details.LastPhotoURL = string.Format("{0}://{1}:{2}/dzc/{3}/{4}.phocalstream/Tiles.dzi", Request.Url.Scheme,
                Request.Url.Host,
                Request.Url.Port,
                site.Name,
                PhotoEntityRepository.First(p => p.ID == details.LastPhotoID).BlobID);

            return PartialView("_SiteDetails", details);
        }
    }
}
