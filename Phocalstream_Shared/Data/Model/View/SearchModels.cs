using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

namespace Phocalstream_Shared.Data.Model.View
{
    public class SearchModel
    {
        public string UserId { get; set; }

        public string CollectionId { get; set; }

        public bool CameraSites { get; set; }

        public bool PublicUserCollections { get; set; }

        [Display(Name = "Sites")]
        public string Sites { get; set; }

        [Display(Name = "Dates")]
        public string Dates { get; set; }

        [Display(Name = "Tags")]
        public string Tags { get; set; }

        public string Months { get; set; }
        
        public string Hours { get; set; }

        public string Group { get; set; }

        public ICollection<string> SiteNames { get; set; }
        public ICollection<string> AvailableTags { get; set; }
        public UserCollectionList UserCollections { get; set; }

        public bool IsEmpty()
        {
            return String.IsNullOrWhiteSpace(Sites)
                && String.IsNullOrWhiteSpace(Tags)
                && String.IsNullOrWhiteSpace(Dates)
                && String.IsNullOrWhiteSpace(Hours) 
                && String.IsNullOrWhiteSpace(Months);
        }
    }
}