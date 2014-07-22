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

namespace Phocalstream_Web.Controllers
{
    public class SearchController : Controller
    {
        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IEntityRepository<Photo> PhotoRepository { get; set; }

        [Dependency]
        public IUnitOfWork Unit { get; set; }

        [Dependency]
        public IPhotoService PhotoService { get; set; }

        [Dependency]
        public ISearchService SearchService { get; set; }


        public ActionResult Index()
        {
            return View();
        }

        public ActionResult AdvancedSearch(SearchModel model)
        {
            Guid containerID = Guid.NewGuid();
            
            string search_path = SearchService.ValidateAndGetSearchPath();
            string collection_path = Path.Combine(search_path, containerID.ToString(), "collection.dzc");


            List<Photo> matches = new List<Photo>();

            Collection c = new Collection();
            c.Name = string.Format("Collection Created {0}", DateTime.Today.ToString("MM/dd/yyyy"));
            c.ContainerID = containerID.ToString();
            c.Type = CollectionType.SEARCH;

            CollectionRepository.Insert(c);
            Unit.Commit();


            matches.AddRange(SearchService.GetPhotosByDate(model.Date));
            matches.AddRange(SearchService.GetPhotosByTag(model.Tags));
            matches = matches.Distinct().OrderBy(p => p.ID).ToList<Photo>();
            
            List<string> fileNames = new List<string>();
            StringBuilder photoIds = new StringBuilder();
            
            foreach (Photo photo in matches)
            {
                fileNames.Add(Path.Combine(PathManager.GetPhotoPath(), photo.Site.DirectoryName,
                    string.Format("{0}.phocalstream", photo.BlobID), "Tiles.dzi"));

                photoIds.Append(photo.ID.ToString() + ",");
            }

            photoIds.Remove(photoIds.Length - 1, 1);


            c.Photos = matches;
            Unit.Commit();

            
            SearchService.GenerateCollectionManifest(fileNames, collection_path);
            PhotoService.GeneratePivotManifest(containerID.ToString(), photoIds.ToString());

            return RedirectToAction("SearchResult", new { collectionID = c.ID });
        }

        public ActionResult SearchResult(int collectionID)
        {
            SearchResults model = new SearchResults();
            
            Collection c = CollectionRepository.First(col => col.ID == collectionID);
            model.Query = c.Name.Split()[0];

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
