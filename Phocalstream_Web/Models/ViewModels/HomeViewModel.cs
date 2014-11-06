using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Web;
using Phocalstream_Shared.Data.Model.View;
using Phocalstream_Shared.Model.View;

namespace Phocalstream_Web.Models.ViewModels
{
    public class HomeViewModel
    {
        public ICollection<ThumbnailModel> PublicCollectionThumbnails { get; set; }
        public ICollection<Collection> Collections { get; set; }
        public ICollection<SiteDetails> Sites { get; set; }
        public ICollection<ThumbnailModel> SiteThumbnails { get; set; }
        public int SiteIndex { get; set; }
        public List<string> Tags { get; set; }
    }
}