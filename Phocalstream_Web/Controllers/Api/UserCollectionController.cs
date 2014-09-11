using Microsoft.Practices.Unity;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;

namespace Phocalstream_Web.Controllers.Api
{
    public class UserCollectionController : ApiController
    {
        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IEntityRepository<Photo> PhotoRepository { get; set; }

        [Dependency]
        public IEntityRepository<User> UserRepository { get; set; }

        [Dependency]
        public IUnitOfWork Unit { get; set; }

        [Dependency]
        public IPhotoService PhotoService { get; set; }

        [Dependency]
        public ICollectionService CollectionService { get; set; }


        [HttpPost, ActionName("SaveUserCollection")]
        public void SaveUserCollection(string collectionName, string photoIds)
        {
            long[] ids = photoIds.Split(',').Select(i => Convert.ToInt64(i)).ToArray();
            List<Photo> photos = PhotoRepository.Find(p => ids.Contains(p.ID)).ToList();

            Guid containerID = Guid.NewGuid();

            //save the collection
            Collection c = new Collection();
            c.Name = collectionName;
            c.ContainerID = containerID.ToString();
            c.Owner = UserRepository.First(u => u.GoogleID == this.User.Identity.Name);
            c.Type = CollectionType.USER;
            c.Photos = photos;
            CollectionRepository.Insert(c);
            Unit.Commit();

            //generate xml manifests
            string collectionPath = CollectionService.ValidateAndGetUserCollectionPath();
            CollectionService.GenerateCollectionManifest(PhotoService.GetFileNames(photos),
                Path.Combine(collectionPath, containerID.ToString(), "collection.dzc"));
            PhotoService.GeneratePivotManifest(collectionPath, containerID.ToString(), String.Join(",", ids), CollectionType.USER);
        }

    }
}