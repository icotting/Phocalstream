using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Data.Model.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class TagViewModel
    {
        public IEnumerable<Tag> Tags { get; set; }
        public IEnumerable<ThumbnailModel> TagThumbnails { get; set; }
    }
}