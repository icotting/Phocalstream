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
            User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            CollectionService.NewUserCollection(user, collectionName, photoIds);
        }

        [HttpPost, ActionName("AddToCollection")]
        public void AddToExistingUserCollection(string collectionIds, string photoIds)
        {
            User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            CollectionService.AddToExistingUserCollection(user, collectionIds, photoIds);
        }

    }
}