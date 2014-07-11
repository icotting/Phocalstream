﻿using Phocalstream_Shared.Data.Model.Photo;
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
        void GeneratePivotManifest(string collectionName, string photoList);
    }
}
