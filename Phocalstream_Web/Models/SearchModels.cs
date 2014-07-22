﻿using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models
{
    public class SearchModel
    {
        [Display(Name = "Date")]
        public string Date { get; set; }

        [Display(Name = "Tags")]
        public string Tags { get; set; }

        [Display(Name = "Time of Day")]
        public string TimeOfDay { get; set; }
    }

    public class SearchResults
    {
        public string Query { get; set; }
        public string CollectionUrl { get; set; }
    }

    public class SearchList
    {
        public List<Collection> Collections { get; set; }
        public string SearchPath { get; set; }
    }
}