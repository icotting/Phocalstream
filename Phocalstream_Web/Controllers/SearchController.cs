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
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Data.Entity;
using Phocalstream_Shared.Data.Model.View;
using Phocalstream_Web.Models.ViewModels;

namespace Phocalstream_Web.Controllers
{
    public class SearchController : Controller
    {
        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IEntityRepository<Photo> PhotoRepository { get; set; }

        [Dependency]
        public IEntityRepository<User> UserRepository { get; set; }

        [Dependency]
        public IPhotoRepository PhotoRepo { get; set; }

        [Dependency]
        public IUnitOfWork Unit { get; set; }

        [Dependency]
        public IPhotoService PhotoService { get; set; }

        [Dependency]
        public ISearchService SearchService { get; set; }

        [Dependency]
        public ICollectionService CollectionService { get; set; }


        public ActionResult Index(long collectionId = -1, string tag = "", string site = "", string year = "")
        {
            SearchModel model = new SearchModel();

            model.AvailableTags = PhotoService.GetTagNames();
            model.SiteNames = SearchService.GetSiteNames();

            Phocalstream_Shared.Data.Model.Photo.User User = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            if (User != null)
            {
                UserCollectionList userCollectionModel = new UserCollectionList();
                userCollectionModel.User = User;
                userCollectionModel.Collections = CollectionRepository.Find(c => c.Owner.ID == User.ID && c.Site == null && c.Type == CollectionType.USER, c => c.Photos).ToList();
                model.UserCollections = userCollectionModel;

                ViewBag.UserId = User.ID;
            }


            Collection collection = CollectionRepository.Find(c => c.ID == collectionId).FirstOrDefault();
            if (collection != null && !collection.Public && collection.Type == CollectionType.USER)
            {
                // If the collection is a user collection, and it does not belong to current user
                if (User == null || collection.Owner.ID != User.ID)
                {
                    collection = null;
                }
            }

            if (collection != null)
            {
                ViewBag.CollectionId = collection.ID;
            }
            
            ViewBag.Tag = tag;
            ViewBag.Site = site;
            ViewBag.Year = year;

            return View(model);
        }
    }
}
