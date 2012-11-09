using Phocalstream_Shared;
using Phocalstream_Shared.Models;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Phocalstream_Web.Controllers
{
    public class SearchController : Controller
    {
        //
        // GET: /Search/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Search(string query)
        {
            SearchResults results = new SearchResults();
            results.Results = new List<SearchResult>();

            List<Photo> matches = null;
            using ( EntityContext ctx = new EntityContext()) 
            {
                try
                {
                    DateTime dateQuery = DateTime.Parse(query);
                    matches = ctx.Photos.Where(p => p.Captured.Month == dateQuery.Month && 
                        p.Captured.Day == dateQuery.Day && 
                        p.Captured.Year == dateQuery.Year).ToList<Photo>();
                }
                catch (Exception e)
                {
                    matches = new List<Photo>();
                }
                foreach ( Photo photo in matches )
                {
                    SearchResult result = new SearchResult();
                    result.ImageUrl = string.Format("{0}://{1}:{2}/dzc/{3}-dz/{4}.dzi", Request.Url.Scheme,
                    Request.Url.Host,
                    Request.Url.Port,
                    photo.Site.ContainerID,
                    photo.BlobID);

                    result.Photo = photo;
                    results.Results.Add(result);
                }
            }
            return PartialView("_SearchResults", results);
        }

    }
}
