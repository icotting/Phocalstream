using Microsoft.DeepZoomTools;
using Microsoft.Practices.Unity;
using Phocalstream_Service.Service;
using Phocalstream_Shared;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Service;
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

        public ActionResult Index(string query)
        {
            SearchResults results = new SearchResults();
            results.Results = new List<SearchResult>();
            results.Query = query;

            //get query hash and name the container (becomes the file name)
            int qid = query.GetHashCode();
            string containerID = string.Format("search{0}", qid);

            //the root path where searchs are stored
            string search_path = ConfigurationManager.AppSettings["searchPath"];
            if (!Directory.Exists(search_path))
            {
                Directory.CreateDirectory(search_path);
            }
            //replace this
            //string basePath = ConfigurationManager.AppSettings["photoPath"];
            
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

                XmlDocument doc = new XmlDocument();
                XmlElement root = doc.CreateElement("Collection");
                root.SetAttribute("MaxLevel", "7");
                root.SetAttribute("TileSize", "256");
                root.SetAttribute("Format", "jpg");
                root.SetAttribute("xmlns", "http://schemas.microsoft.com/deepzoom/2009");

                doc.AppendChild(root);

                XmlDeclaration xmldecl = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
                doc.InsertBefore(xmldecl, root);

                XmlElement items = doc.CreateElement("Items");
                int count = 0;
                List<string> fileNames = new List<string>();
                StringBuilder photoIds = new StringBuilder();

                foreach (Photo photo in matches)
                {
                    fileNames.Add(Path.Combine(ConfigurationManager.AppSettings["photoPath"], photo.Site.DirectoryName,
                        string.Format("{0}.phocalstream", photo.BlobID), "Tiles.dzi"));

                    photoIds.Append(photo.ID.ToString() + ",");

                    //navigate up two directores, then come into the photo directory
                    string photoRelativePath = string.Format(@"../{0}/{1}.phocalstream/Tiles.dzi", photo.Site.DirectoryName, photo.BlobID);

                    XmlElement item = doc.CreateElement("I");
                    item.SetAttribute("Source", photoRelativePath);
                    item.SetAttribute("N", Convert.ToString(count));
                    item.SetAttribute("Id", Convert.ToString(count++));

                    XmlElement size = doc.CreateElement("Size");
                    size.SetAttribute("Width", Convert.ToString(photo.Width));
                    size.SetAttribute("Height", Convert.ToString(photo.Height));
                    item.AppendChild(size);

                    items.AppendChild(item);

                    SearchResult result = new SearchResult();
                    result.ImageUrl = string.Format("{0}://{1}:{2}/dzc/{3}-dz/{4}.dzi", Request.Url.Scheme,
                    Request.Url.Host,
                    Request.Url.Port,
                    photo.Site.ContainerID,
                    photo.BlobID);

                    result.Photo = photo;
                    results.Results.Add(result);
                }

                photoIds.Remove(photoIds.Length - 1, 1);

                c.Photos = matches;
                Unit.Commit();

                creator.Create(new List<string>(fileNames), Path.Combine(search_path, containerID, "collection.dzc"));

                root.SetAttribute("NextItemId", Convert.ToString(count));
                root.AppendChild(items);
                doc.Save(Path.Combine(search_path, containerID, "collection1.dzc"));

                PhotoService.GeneratePivotManifest(containerID, photoIds.ToString());
            }

            results.CollectionUrl = string.Format("{0}://{1}:{2}/api/sitecollection/pivotcollectionfor?id={3}", Request.Url.Scheme,
                Request.Url.Host,
                Request.Url.Port,
                c.ID);

            return View(results);
        }

        public ActionResult List()
        {
            List<Collection> SearchCollections = CollectionRepository.Find(c => c.Type == CollectionType.SEARCH).ToList();



            return View();
        }
    }
}
