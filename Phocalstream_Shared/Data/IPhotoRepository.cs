using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Data.Model.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Phocalstream_Shared.Data
{
    public interface IPhotoRepository
    {
        SiteDetails GetSiteDetails(CameraSite site);
        XmlDocument CreateDeepZoomForSite(long siteID);
        XmlDocument CreateDeepZomForList(string photoList);
    }
}
