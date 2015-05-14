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
        List<string> GetSiteNames();
        List<long> SearchResultPhotoIds(SearchModel model);
    }
}
