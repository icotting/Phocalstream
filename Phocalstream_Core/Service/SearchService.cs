using Microsoft.DeepZoomTools;
using Microsoft.Practices.Unity;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace Phocalstream_Service.Service
{
    public class SearchService : ISearchService
    {
        [Dependency]
        public IEntityRepository<Photo> PhotoRepository { get; set; }

        [Dependency]
        public IEntityRepository<Tag> TagRepository { get; set; }

        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IUnitOfWork Unit { get; set; }

        
        public string ValidateAndGetSearchPath()
        {
            string search_path = PathManager.GetSearchPath();
            if(!Directory.Exists(search_path))
            {
                Directory.CreateDirectory(search_path);
            }

            return search_path;
        }

        public void DeleteSearch(long collectionID)
        {
            Collection col = CollectionRepository.First(c => c.ID == collectionID && c.Type == CollectionType.SEARCH);

            if (col != null)
            {
                string filePath = Path.Combine(PathManager.GetSearchPath(), col.ContainerID);

                if (System.IO.Directory.Exists(filePath))
                {
                    System.IO.Directory.Delete(filePath, true);
                }

                CollectionRepository.Delete(col);
                Unit.Commit();
            }
        }

        public void DeleteAllSearches()
        {
            List<Collection> collections = CollectionRepository.Find(c => c.Type == CollectionType.SEARCH).ToList();

            foreach (var col in collections)
            {
                string filePath = Path.Combine(PathManager.GetSearchPath(), col.ContainerID);

                if (System.IO.Directory.Exists(filePath))
                {
                    System.IO.Directory.Delete(filePath, true);
                }

                CollectionRepository.Delete(col);
                Unit.Commit();
            }
        }

        public void GenerateCollectionManifest(List<string> fileNames, string savePath)
        {
            CollectionCreator creator = new CollectionCreator();
            creator.TileFormat = Microsoft.DeepZoomTools.ImageFormat.Jpg;
            creator.TileOverlap = 1;
            creator.TileSize = 256;

            creator.Create(fileNames, savePath);
        }

        public List<Photo> GetPhotosByDate(string dateString)
        { 
            List<Photo> matches = new List<Photo>();

            if (dateString != null)
            {
                string[] dates = dateString.Split(',');

                foreach (var d in dates)
                {
                    DateTime date = DateTime.Parse(d);
                    matches.AddRange(PhotoRepository.Find(p => p.Captured.Month == date.Month &&
                            p.Captured.Day == date.Day &&
                            p.Captured.Year == date.Year));
                }
            }

            return matches;
        }

        public List<Photo> GetPhotosByTag(string tagString)
        {
            List<Photo> matches = new List<Photo>();

            if (tagString != null)
            {
                string[] tags = tagString.Split(',');

                IEnumerable<Photo> photos = PhotoRepository.GetAll(p => p.Tags);
                foreach (var tagName in tags)
                {
                    Tag tag = TagRepository.First(t => t.Name.Equals(tagName));
                    matches.AddRange(photos.Where(p => p.Tags.Contains(tag)));
                }
            }

            return matches;
        }
    }
}
