using Phocalstream_Shared;
using Phocalstream_Shared.Models;
using Phocalstream_Web.Application;
using Phocalstream_Web.Models;
using Phocalstream_Web.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Phocalstream_Web.Controllers
{
    public class PhotoController : Controller
    {
        [AllowAnonymous]
        public ActionResult Index(long photoID)
        {
            PhotoViewModel model = new PhotoViewModel();
            using (EntityContext ctx = new EntityContext())
            {
                model.Photo = ctx.Photos.Include("Site").SingleOrDefault(p => p.ID == photoID);
            }

            if (model.Photo == null)
            {
                return new HttpNotFoundResult(string.Format("Photo {0} was not found", photoID));
            }

            model.ImageUrl = string.Format("{0}://{1}:{2}/dzc/{3}-dz/{4}.dzi", Request.Url.Scheme,
                    Request.Url.Host,
                    Request.Url.Port,
                    model.Photo.Site.ContainerID,
                    model.Photo.BlobID);
            model.PhotoDate = model.Photo.Captured.ToString("MMM dd, yyyy");
            model.PhotoTime = model.Photo.Captured.ToString("h:mm:ss tt");
            model.SiteCoords = string.Format("{0}, {1}", model.Photo.Site.Latitude, model.Photo.Site.Longitude);

            return View(model);
        }

        [AllowAnonymous]
        public ActionResult CameraCollection(long siteID)
        {
            using (EntityContext ctx = new EntityContext())
            {
                CollectionViewModel model = new CollectionViewModel();
                model.Collection = (from c in ctx.Collections where c.Site.ID == siteID select c).First();
                model.CollectionUrl = string.Format("{0}://{1}:{2}/api/sitecollection/{3}", Request.Url.Scheme,
                    Request.Url.Host,
                    Request.Url.Port,
                    model.Collection.Site.ID);
                model.SiteCoords = string.Format("{0}, {1}", model.Collection.Site.Latitude, model.Collection.Site.Longitude);

                List<Photo> photos = ctx.Photos.Where(p => p.Site.ID == model.Collection.Site.ID).OrderBy(p => p.Captured).ToList<Photo>();
                model.PhotoCount = photos.Count();
                model.StartDate = photos.Select(p => p.Captured).First().ToString(@"MMM dd, yyyy");
                model.EndDate = photos.Select(p => p.Captured).Last().ToString(@"MMM dd, yyyy");
                return View(model);
            }
        }

    }
}
