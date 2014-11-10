using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Data.Model.View;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{

    public class SearchResults
    {
        public Collection Collection { get; set; }

        public SearchResultPartial Partial { get; set; }

        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime First { get; set; }

        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime Last { get; set; }

        public string PhotoIdList { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,#}")]
        public int PhotoCount { get; set; }
        public string CollectionUrl { get; set; }
        public UserCollectionList UserCollections { get; set; }
    }

    public class SearchResultPartial
    {
        public long CollectionID { get; set; }
        public int Index { get; set; }
        public int TotalPages { get; set; }
        public string Description { get; set; }
        public IEnumerable<Photo> Photos { get; set; }
        public int PhotosPerPage { get; set; }
    }

    public class SearchList
    {
        public List<Collection> Collections { get; set; }
        public string SearchPath { get; set; }
    }
}