using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Service
{
    public interface ICollectionService
    {
        string ValidateAndGetUserCollectionPath(); 
        void DeleteUserCollection(long collectionID);
        void DeleteUserCollections(long userID);
        void DeleteAllUserCollections();
        void GenerateCollectionManifest(List<string> fileNames, string savePath);
        void NewUserCollection(User user, string collectionName, string photoIds);
        void AddToExistingUserCollection(User user, string collectionIds, string photoIds);
        void RemoveFromExistingUserCollection(User user, long collectionID, string photoIDs);
        void TogglePhotoInUserCollection(long photoID, long collectionID);
        void UpdateUserCollection(Collection collection);
        void SetUserCollectionPublic(User user, long collectionID, bool publish);
        
    }
}