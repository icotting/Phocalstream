using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Phocalstream_Shared.Service
{
    public interface IPhotoService
    {
        Collection GetCollectionForProcessing(XmlNode siteData);
        Photo ProcessPhoto(string fileName, CameraSite site);
        void ProcessCollection(Collection collection);
        void GeneratePivotManifest(CameraSite site);

        void GenerateSubSetManifest(CameraSite site, string subsetName, string photoList);
        
        void GeneratePivotManifest(string basePath, string collectionID, string photoList, CollectionType type);

        List<string> GetUnusedTagNames(long photoID);
        List<string> GetTagNames();
        Photo AddTag(long photoID, string tags);
        List<Tuple<string, int>> GetPopularTagsForSite(long siteID);
        List<string> GetFileNames(List<Photo> photos);
    }
}
