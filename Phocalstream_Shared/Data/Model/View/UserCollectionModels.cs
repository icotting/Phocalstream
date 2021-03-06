﻿using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Model.View;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data.Model.View
{
    public class UserCollectionList
    {
        public User User { get; set; }
        public IEnumerable<Collection> Collections { get; set; }
        public ICollection<ThumbnailModel> SiteThumbnails { get; set; }
        public ICollection<ThumbnailModel> TimelapseThumbnails { get; set; }
        public ICollection<ThumbnailModel> CollectionThumbnails { get; set; }


    }

    public class EditUserCollection
    {
        public Collection Collection { get; set; }
        public long CoverPhotoId { get; set; }
    }

    public class UserPhotoUpload
    {
        public List<Collection> UserSiteCollections { get; set; }
    }

    public class AddUserCameraSite
    {
        [Required]
        [StringLength(2)]
        public string CameraSiteName { get; set; }
        
        [Required]
        [RegularExpression(@"^-?([1-8]?[1-9]|[1-9]0)\.{1}\d{1,6}")]
        public double Latitude { get; set; }

        [Required]
        [RegularExpression(@"^-?([1-8]?[1-9]|[1-9]0)\.{1}\d{1,6}")]
        public double Longitude { get; set; }

        public string County { get; set; }
        public string State { get; set; }
        
        public bool Public { get; set; }
    }

}
