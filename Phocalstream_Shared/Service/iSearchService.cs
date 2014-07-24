using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Data.Model.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Service
{
    public interface ISearchService
    {
        string ValidateAndGetSearchPath();
        void DeleteSearch(long collectionID);
        void DeleteAllSearches();
        void GenerateCollectionManifest(List<string> fileNames, string savePath);
        List<string> GetSiteNames();
        SearchMatches Search(SearchModel model);
        List<Photo> GetPhotosBySite(string siteString);
        List<Photo> GetPhotosBySeason(string seasonString);
        List<Photo> GetPhotosByMonth(string monthString);
        List<Photo> GetPhotosByDate(string dateString);
        List<Photo> GetPhotosByTag(string tagString);
        List<Photo> GetPhotosByTimeOfDay(string timeString);
        List<Photo> GetPhotosByHourOfDay(string hourString);
    }
}
