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

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Search(string query)
        {
            //get query hash and name the container (becomes the file name)
            int qid = query.GetHashCode();
            string containerID = string.Format("{0}", qid);

            //the root path where searchs are stored
            string search_path = PathManager.GetSearchPath();
            if (!Directory.Exists(search_path))
            {
                Directory.CreateDirectory(search_path);
            }
            
            List<Photo> matches = null;

            Collection c = CollectionRepository.First(col => col.ContainerID == containerID);
            if (c == null)
            {
                c = new Collection();
                c.Name = string.Format("{0} results", query);
                c.ContainerID = containerID;
                c.Photos = matches;
                c.Type = CollectionType.SEARCH;

                CollectionRepository.Insert(c);
                Unit.Commit();

                try
                {
                    DateTime dateQuery = DateTime.Parse(query);
                    matches = PhotoRepository.Find(p => p.Captured.Month == dateQuery.Month &&
                        p.Captured.Day == dateQuery.Day &&
                        p.Captured.Year == dateQuery.Year).ToList<Photo>();
                }
                catch (Exception e)
                {
                    matches = new List<Photo>();
                }

                CollectionCreator creator = new CollectionCreator();
                creator.TileFormat = Microsoft.DeepZoomTools.ImageFormat.Jpg;
                creator.TileOverlap = 1;
                creator.TileSize = 256;

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

                creator.Create(new List<string>(fileNames), Path.Combine(search_path, containerID, "collection.dzc"));

                PhotoService.GeneratePivotManifest(containerID, photoIds.ToString());
            }
           
            return RedirectToAction("SearchResult", new {collectionID = c.ID});
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
            List<Collection> collections;
            if (collectionID == -1)
            {
                collections = CollectionRepository.Find(c => c.Type == CollectionType.SEARCH).ToList();
            }
            else
            {
                collections = CollectionRepository.Find(c => c.ID == collectionID && c.Type == CollectionType.SEARCH).ToList();

            }

            foreach(var col in collections)
            {
                string filePath = Path.Combine(PathManager.GetSearchPath(), col.ContainerID);

                if (System.IO.Directory.Exists(filePath))
                {
                    System.IO.Directory.Delete(filePath, true);
                }

                CollectionRepository.Delete(col);
                Unit.Commit();
            }
            
            return RedirectToAction("List", "Search");
        }
    }
}
