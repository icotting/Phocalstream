using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.IO;
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
        Photo ProcessUserPhoto(Stream stream, string fileName, User user, long collectionID);
        List<string> GetUnusedTagNames(long photoID);
        List<string> GetTagNames();
        Photo AddTag(long photoID, string tags);
        List<Tuple<string, int, long>> GetPopularTagsForSite(long siteID);
        List<string> GetFileNames(List<Photo> photos);
        ICollection<TimeLapseFrame> CreateTimeLapseFramesFromIDs(long[] photoIDs);
    }
}
