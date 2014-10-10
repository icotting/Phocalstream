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

        [Dependency]
        public IPhotoService PhotoService { get; set; }


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

        public void NewUserCollection(User user, string collectionName, string photoIds)
        {
            List<Photo> photos;
            long[] ids;
            if (!String.IsNullOrWhiteSpace(photoIds))
            {
                ids = photoIds.Split(',').Select(i => Convert.ToInt64(i)).ToArray();
                photos = PhotoRepository.Find(p => ids.Contains(p.ID), p => p.Site).ToList();
            }
            else
            {
                ids = new long[0];
                photos = new List<Photo>();   
            }

            Guid containerID = Guid.NewGuid();

            //save the collection
            Collection c = new Collection() 
            {
                Name = collectionName,
                ContainerID = containerID.ToString(),
                Owner = user,
                Type = CollectionType.USER,
                Status = CollectionStatus.COMPLETE,
                Photos = photos
            };
            CollectionRepository.Insert(c);
            Unit.Commit();

            //only generate the manifest if there are photos in the collection
            if (photos.Count > 0)
            {
                GenerateManifests(containerID.ToString(), ids, photos);
            }
        }

        public void AddToExistingUserCollection(User user, string collectionIds, string photoIds)
        {
            long[] ids = photoIds.Split(',').Select(i => Convert.ToInt64(i)).ToArray();
            List<Photo> photos = PhotoRepository.Find(p => ids.Contains(p.ID)).ToList();

            long[] cIds = collectionIds.Split(',').Select(i => Convert.ToInt64(i)).ToArray();
            List<Collection> collections = CollectionRepository.Find(c => cIds.Contains(c.ID) && c.Type == CollectionType.USER, c => c.Photos).ToList();

            foreach (var col in collections)
            {
                col.Photos = col.Photos.Union(photos).ToList();
                col.Status = CollectionStatus.INVALID;
            }
            Unit.Commit();
        }

        public void RemoveFromExistingUserCollection(User user, long collectionID, string photoIds)
        {
            long[] ids = photoIds.Split(',').Select(i => Convert.ToInt64(i)).ToArray();
            List<Photo> photos = PhotoRepository.Find(p => ids.Contains(p.ID)).ToList();

            Collection collection = CollectionRepository.First(c => c.ID == collectionID && c.Type == CollectionType.USER, c => c.Photos);

            collection.Photos = collection.Photos.Except(photos).ToList();
            collection.Status = CollectionStatus.INVALID;
            Unit.Commit();
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

                col.Status = CollectionStatus.INVALID;
                Unit.Commit();
            }
        }
    
        public void UpdateUserCollection(Collection collection)
        {
            DeleteManifests(collection.ContainerID);
            GenerateManifests(collection.ContainerID, collection.Photos.Select(p => p.ID).ToArray(), collection.Photos.ToList());

            collection.Status = CollectionStatus.COMPLETE;
            Unit.Commit();
        }

        private void DeleteManifests(string containerID)
        {
            string collectionPath = ValidateAndGetUserCollectionPath();

            string collectionManifestPath = Path.Combine(collectionPath, containerID, "collection.dzc");
            if (File.Exists(collectionManifestPath))
            {
                File.Delete(collectionManifestPath);
            }

            string pivotManifestPath = Path.Combine(collectionPath, containerID, "site.cxml");
            if (File.Exists(pivotManifestPath))
            {
                File.Delete(pivotManifestPath);
            }
        }

        private void GenerateManifests(string containerID, long[] ids, List<Photo> photos)
        {
            string collectionPath = ValidateAndGetUserCollectionPath();
            GenerateCollectionManifest(PhotoService.GetFileNames(photos),
                Path.Combine(collectionPath, containerID, "collection.dzc"));
            PhotoService.GeneratePivotManifest(collectionPath, containerID, String.Join(",", ids), CollectionType.USER);
        }

        public void SetUserCollectionCoverPhoto(User user, long collectionID, long photoID)
        {
            Collection collection = CollectionRepository.First(c => c.ID == collectionID && c.Owner.ID == user.ID);
            Photo photo = PhotoRepository.First(p => p.ID == photoID);

            collection.CoverPhoto = photo;
            Unit.Commit();
        }

    }
}
