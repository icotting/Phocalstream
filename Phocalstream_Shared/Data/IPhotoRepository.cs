﻿using Phocalstream_Shared.Data.Model.Photo;
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
        TagDetails GetTagDetails(Tag Tag);
        
        /* These methods should be split off into a service */
        XmlDocument CreateDeepZoomForSite(long siteID);
        XmlDocument CreateDeepZomForList(string photoList);
        XmlDocument CreatePivotCollectionForSite(long siteID);
        XmlDocument CreatePivotCollectionForList(string collectionName, string photoList, CollectionType type, string subsetName = null);
    }
}
