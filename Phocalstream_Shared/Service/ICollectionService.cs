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
        
    }
}