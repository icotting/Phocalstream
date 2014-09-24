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
        List<string> GetSiteNames();
        int SearchResultCount(SearchModel model);
        long SearchResultPhotoId(SearchModel model);
        void ValidateCache(SearchModel model, int currentCount);
        SearchMatches Search(SearchModel model);
    }
}
