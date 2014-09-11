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

        [Dependency]
        public ICollectionService CollectionService { get; set; }


        public ActionResult Index(int e = 0)
        {
            SearchModel model = new SearchModel();

            Collection first = CollectionRepository.Find(c => c.Type == CollectionType.SITE).OrderBy(c => Guid.NewGuid()).First();
            model.BackgroundImageID = first.CoverPhoto == null ? PhotoRepo.GetSiteDetails(first.Site).LastPhotoID : first.CoverPhoto.ID;

            model.AvailableTags = PhotoService.GetTagNames();
            model.SiteNames = SearchService.GetSiteNames();

            if (e == 1)
            {
                ViewBag.Message = "Zero photos matched those parameters, please try again.";
            }
            else if (e == 2)
            {
                ViewBag.Message = "Please enter at least one (1) search parameter.";
            }

            return View(model);
        }
        
        public ActionResult TagSearch(string tag)
        {
            if (String.IsNullOrWhiteSpace(tag))
            {
                return RedirectToAction("Index", "Home", new { e = 2 });
            }
            else
            {
                SearchModel model = new SearchModel();
                model.Tags = tag;

                return RedirectToAction("AdvancedSearch", model);
            }
        }

        public ActionResult KnockoutAdvancedSearch(string hours, string months, string sites, string tags, string dates)
        {
            SearchModel model = new SearchModel();

            model.Sites = sites;
            model.Tags = tags;
            model.Dates = dates;
            model.Hours = hours;
            model.Months = months;

            return RedirectToAction("AdvancedSearch", model);
        }

        public ActionResult AdvancedSearch(SearchModel model)
        {
            //check if the model is empty
            if (model.IsEmpty())
            {
                return RedirectToAction("Index", new { e = 2 });
            }

            var collectionName = model.CreateCollectionName();
            var containerID = collectionName.GetHashCode().ToString();

            //check if the model exists
            Collection existingCollection = CollectionRepository.Find(c => c.ContainerID == containerID).FirstOrDefault();
            if (existingCollection != null)
            {
                return RedirectToAction("SearchResult", new { collectionID = existingCollection.ID });
            }
            //else, execute the search
            else
            {
                SearchMatches result = SearchService.Search(model);

                //if search yielded result, do proceed
                if (result.Ids.Count > 0)
                {
                    //save the collection
                    Collection c = new Collection();
                    c.Name = collectionName;
                    c.ContainerID = containerID;
                    c.Type = CollectionType.SEARCH;
                    c.Photos = result.Matches;
                    CollectionRepository.Insert(c);
                    Unit.Commit();

                    //generate xml manifests
                    string searchPath = SearchService.ValidateAndGetSearchPath();
                    CollectionService.GenerateCollectionManifest(PhotoService.GetFileNames(result.Matches), 
                        Path.Combine(searchPath, containerID.ToString(), "collection.dzc"));
                    PhotoService.GeneratePivotManifest(searchPath, containerID.ToString(), String.Join(",", result.Ids.ToArray()), CollectionType.SEARCH);

                    return RedirectToAction("SearchResult", new { collectionID = c.ID });
                }
                //else, redirect back to search page
                else
                {
                    return RedirectToAction("Index", new { e = 1 });
                }
            }
        }

        public ActionResult SearchResult(int collectionID)
        {
            SearchResults model = new SearchResults();
            
            Collection c = CollectionRepository.First(col => col.ID == collectionID, col => col.Photos);
            model.CollectionName = c.Name;

            model.PhotoCount = c.Photos.Count;

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
