using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models
{
    public class SearchResults
    {
        public string Query { get; set; }
        public List<SearchResult> Results { get; set; }
        public string CollectionUrl { get; set; }
    }

    public class SearchResult
    {
        public string ImageUrl { get; set; }
        public Photo Photo { get; set; }
    }

    public class SearchCache
    {

    }

    public class SearchList
    {
        public List<Collection> Collections { get; set; }
    }
}