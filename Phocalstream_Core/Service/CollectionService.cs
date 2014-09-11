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

namespace Phocalstream_Service.Service
{
    public class CollectionService : ICollectionService
    {
        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IEntityRepository<Photo> PhotoRepository { get; set; }

        [Dependency]
        public IUnitOfWork Unit { get; set; }


        public string ValidateAndGetUserCollectionPath()
        {
            string collection_path = PathManager.GetUserCollectionPath();
            if (!Directory.Exists(collection_path))
            {
                Directory.CreateDirectory(collection_path);
            }

            return collection_path;
        }

        public void DeleteUserCollection(long collectionID)
        {
            Collection col = CollectionRepository.First(c => c.ID == collectionID && c.Type == CollectionType.USER);

            if (col != null)
            {
                string filePath = Path.Combine(ValidateAndGetUserCollectionPath(), col.ContainerID);

                if (System.IO.Directory.Exists(filePath))
                {
                    System.IO.Directory.Delete(filePath, true);
                }

                CollectionRepository.Delete(col);
                Unit.Commit();
            }
        }

        public void DeleteUserCollections(long userID)
        {
            IEnumerable<Collection> collections = CollectionRepository.Find(c => c.Owner.ID == userID && c.Type == CollectionType.USER);

            var collectionPath = ValidateAndGetUserCollectionPath();
            foreach (var col in collections)
            {
                string filePath = Path.Combine(collectionPath, col.ContainerID);

                if (System.IO.Directory.Exists(filePath))
                {
                    System.IO.Directory.Delete(filePath, true);
                }

                CollectionRepository.Delete(col);
                Unit.Commit();
            }
        }

        //Do these delete methods need to be secured?
        public void DeleteAllUserCollections()
        {
            IEnumerable<Collection> collections = CollectionRepository.Find(c => c.Type == CollectionType.USER);

            var collectionPath = ValidateAndGetUserCollectionPath();
            foreach (var col in collections)
            {
                string filePath = Path.Combine(collectionPath, col.ContainerID);

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

        public void TogglePhotoInUserCollection(long photoID, long collectionID)
        {
            Collection col = CollectionRepository.First(c => c.ID == collectionID, c => c.Photos);

            if (col.Type == CollectionType.USER)
            {
                Photo photo = PhotoRepository.Find(photoID);

                if (col.Photos.Contains(photo))
                {
                    col.Photos.Remove(photo);
                }
                else
                {
                    col.Photos.Add(photo);
                }

                Unit.Commit();
            }
        }
    }
}
