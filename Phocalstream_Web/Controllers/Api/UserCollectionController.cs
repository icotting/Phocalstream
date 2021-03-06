﻿using Microsoft.Practices.Unity;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Service;
using Phocalstream_Web.Models.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;

namespace Phocalstream_Web.Controllers.Api
{
    [Authorize]
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

        [Dependency]
        public IDroughtMonitorRepository DroughtMonitorRepository { get; set; }


        [HttpGet, ActionName("UserSites")]
        public List<UserSite> GetUserSites()
        {
            var user = UserRepository.Find(u => u.ProviderID == User.Identity.Name).FirstOrDefault();
            Debug.Assert(user != null);

            var collections = CollectionRepository.Find(c => c.Owner.ID == user.ID & c.Site != null & c.Photos.Count != 0, c => c.Owner, c => c.Photos, c => c.CoverPhoto);
            return collections.Select(c => new UserSite
            {
                CollectionID = c.ID,
                CoverPhotoID = c.Photos.Last().ID,
                From = c.Photos.First().Captured,
                To = c.Photos.Last().Captured,
                Name = c.Name,
                PhotoCount = c.Photos.Count
            }).ToList<UserSite>();
        }

        [HttpPost, ActionName("CreateUserCameraSite")]
        public long CreateUserCameraSite(NewUserSiteModel model)
        {
            User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            string guid = Guid.NewGuid().ToString();

            CameraSite newCameraSite = new CameraSite()
            {
                Name = model.SiteName,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                CountyFips = DroughtMonitorRepository.GetFipsForCountyAndState(model.County, model.State),
                ContainerID = guid,
                DirectoryName = guid
            };

            Collection newCollection = new Collection()
            {
                Name = model.SiteName,
                Site = newCameraSite,
                Owner = user,
                ContainerID = guid,
                Type = CollectionType.USER
            };

            CollectionRepository.Insert(newCollection);
            Unit.Commit();

            return newCollection.ID;
        }

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

        [HttpPost, ActionName("RemoveFromCollection")]
        public void RemoveFromExistingUserCollection(long collectionID, string photoIds)
        {
            User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            CollectionService.RemoveFromExistingUserCollection(user, collectionID, photoIds);
        }

        [HttpPost, ActionName("SetCoverPhoto")]
        public void SetUserCollectionCoverPhoto(long collectionID, long photoId)
        {
            User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            CollectionService.SetUserCollectionCoverPhoto(user, collectionID, photoId);
        }
         
        [HttpPost, ActionName("PublishUserCollection")]
        public void PublishUserCollection(long collectionID)
        {
            User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            CollectionService.SetUserCollectionPublic(user, collectionID, true);
        }

        [HttpPost, ActionName("UnpublishUserCollection")]
        public void UnpublishUserCollection(long collectionID)
        {
            User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            CollectionService.SetUserCollectionPublic(user, collectionID, false);
        }
    }
}