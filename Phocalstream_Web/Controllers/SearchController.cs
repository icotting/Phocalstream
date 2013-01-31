using Microsoft.DeepZoomTools;
using Microsoft.Practices.Unity;
using Phocalstream_Shared;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Web.Application.Data;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
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

        public ActionResult Index(string query)
        {
            SearchResults results = new SearchResults();
            results.Results = new List<SearchResult>();
            results.Query = query;

            List<Photo> matches = null;
            int qid = query.GetHashCode();
            string basePath = ConfigurationManager.AppSettings["photoPath"];
            string containerID = string.Format("search-{0}", qid);

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

                string collectionPath = Path.Combine(Path.Combine(Path.Combine(basePath, containerID, "DZ"), "collection.dzc"));

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

                foreach (Photo photo in matches)
                {
                    fileNames.Add(Path.Combine(Path.Combine(Path.Combine(ConfigurationManager.AppSettings["photoPath"], photo.Site.ContainerID),
                        "DZ"), string.Format("{0}.dzi", photo.BlobID)));

                    string photoRelativePath = string.Format(@"../../{0}/DZ/{1}.dzi", photo.Site.ContainerID, photo.BlobID);

                    XmlElement item = doc.CreateElement("I");
                    item.SetAttribute("Source", photoRelativePath);
                    item.SetAttribute("N", Convert.ToString(count++));
                    item.SetAttribute("Id", Convert.ToString(photo.ID));

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

                c.Photos = matches;
                Unit.Commit();

                creator.Create(new List<string>(fileNames), collectionPath);

                root.SetAttribute("NextItemId", Convert.ToString(count));
                root.AppendChild(items);
                doc.Save(collectionPath);
            }

            results.CollectionUrl = string.Format("{0}://{1}:{2}/api/sitecollection/pivotcollectionfor?id={3}", Request.Url.Scheme,
                Request.Url.Host,
                Request.Url.Port,
                c.ID);

            return View(results);
        }

    }
}
