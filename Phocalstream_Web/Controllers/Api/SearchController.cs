﻿using Gif.Components;
using Microsoft.Practices.Unity;
using Phocalstream_Service.Service;
using Phocalstream_Shared;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Data.Model.View;
using Phocalstream_Shared.Service;
using Phocalstream_Web.Application;
using Phocalstream_Web.Application.Data;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;


namespace Phocalstream_Web.Controllers.Api
{
    public class SearchController : ApiController
    {
        [Dependency]
        public ISearchService SearchService { get; set; }

        [HttpGet]
        [ActionName("count")]
        public HttpResponseMessage SearchCount(string hours, string months, string sites, string tags, string dates)
        {
            QuickSearchModel model = new QuickSearchModel();
            model.Sites = sites;
            model.Tags = tags;
            model.Dates = dates;
            model.Hours = hours;
            model.Months = months;

            int count = SearchService.SearchResultCount(model);

            HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
            message.Content = new StringContent(Convert.ToString(count));

            return message;
        }

        [HttpGet]
        [ActionName("photoId")]
        public HttpResponseMessage SearchPhotoId(string hours, string months, string sites, string tags, string dates)
        {
            QuickSearchModel model = new QuickSearchModel();
            model.Sites = sites;
            model.Tags = tags;
            model.Dates = dates;
            model.Hours = hours;
            model.Months = months;

            long id = SearchService.SearchResultPhotoId(model);

            HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
            message.Content = new StringContent("/api/photo/high/" + Convert.ToString(id));

            return message;

        }
    }
}