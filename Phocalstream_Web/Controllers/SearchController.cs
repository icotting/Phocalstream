using Microsoft.DeepZoomTools;
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

namespace Phocalstream_Web.Controllers
{
    public class SearchController : Controller
    {
        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IEntityRepository<Photo> PhotoRepository { get; set; }

        [Dependency]
        public IPhotoRepository PhotoRepo { get; set; }

        [Dependency]
        public IUnitOfWork Unit { get; set; }

        [Dependency]
        public IPhotoService PhotoService { get; set; }

        [Dependency]
        public ISearchService SearchService { get; set; }


        public ActionResult Index(int e = 0)
        {
            SearchModel model = new SearchModel();

            Collection first = CollectionRepository.Find(c => c.Type == CollectionType.SITE).OrderBy(c => Guid.NewGuid()).First();
            model.BackgroundImageID = first.CoverPhoto == null ? PhotoRepo.GetSiteDetails(first.Site).LastPhotoID : first.CoverPhoto.ID;

            model.AvailableTags = PhotoService.GetTagNames();
            model.SiteNames = SearchService.GetSiteNames();

            if (e != 0)
            {
                ViewBag.Message = "Zero photos matched those parameters, please try again.";
            }

            return View(model);
        }

        public ActionResult AdvancedSearch(SearchModel model)
        {
            //execute the search
            SearchMatches result = SearchService.Search(model);
            
            //if search yielded result, do proceed
            if (result.Ids.Count > 0)
            {
                Guid containerID = Guid.NewGuid();

                //save the collection
                Collection c = new Collection();
                c.Name = string.Format("Collection Created {0}", DateTime.Today.ToString("MM/dd/yyyy"));
                c.ContainerID = containerID.ToString();
                c.Type = CollectionType.SEARCH;
                c.Photos = result.Matches;
                CollectionRepository.Insert(c);
                Unit.Commit();

                //generate xml manifests
                SearchService.GenerateCollectionManifest(PhotoService.GetFileNames(result.Matches), 
                    Path.Combine(SearchService.ValidateAndGetSearchPath(), containerID.ToString(), "collection.dzc"));
                PhotoService.GeneratePivotManifest(containerID.ToString(), String.Join(",", result.Ids.ToArray()));

                return RedirectToAction("SearchResult", new { collectionID = c.ID });
            }
            else
            {
                return RedirectToAction("Index", new { e = 1 });
            }
        }

        public ActionResult SearchResult(int collectionID)
        {
            SearchResults model = new SearchResults();
            
            Collection c = CollectionRepository.First(col => col.ID == collectionID);
            
            model.CollectionUrl = string.Format("{0}://{1}:{2}/api/sitecollection/pivotcollectionfor?id={3}", Request.Url.Scheme,
                Request.Url.Host,
                Request.Url.Port,
                c.ID);

            return View(model);
        }

        public ActionResult List()
        {
            SearchList model = new SearchList();

            model.SearchPath = PathManager.GetSearchPath();

            List<Collection> collections = CollectionRepository.Find(c => c.Type == CollectionType.SEARCH).ToList();

            model.Collections = collections;

            return View(model);
        }

        public ActionResult DeleteSearch(long collectionID)
        {
            SearchService.DeleteSearch(collectionID);
            return RedirectToAction("List", "Search");
        }

        public ActionResult DeleteAllSearches()
        {
            SearchService.DeleteAllSearches();
            return RedirectToAction("List", "Search");
        }
    }
}
