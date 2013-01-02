using Phocalstream_Shared;
using Phocalstream_Shared.Models;
using Phocalstream_Web.Application;
using Phocalstream_Web.Models;
using Phocalstream_Web.Models;
using Phocalstream_Web.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Phocalstream_Web.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            HomeViewModel model = new HomeViewModel();

            using (EntityContext ctx = new EntityContext())
            {
                model.Collections = ctx.Collections.Include("Site").Where(c => c.Status == CollectionStatus.COMPLETE && c.Type == CollectionType.SITE).ToList<Collection>();
                return View(model);
            }
        }

        public ActionResult SiteDetails(long id)
        {
            CameraSite site;
            SiteDetails details = new SiteDetails();
            using (EntityContext ctx = new EntityContext())
            {
               site = ctx.Sites.FirstOrDefault<CameraSite>(s => s.ID == id);
                details.SiteName = site.Name;
                details.SiteID = site.ID;

                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand command = new SqlCommand("select min(Captured), max(Captured), count(*), max(ID) from Photos where Site_ID = @siteID", conn))
                    {
                        command.Parameters.AddWithValue("@siteID", site.ID);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                details.First = reader.GetDateTime(0);
                                details.Last = reader.GetDateTime(1);
                                details.PhotoCount = reader.GetInt32(2);

                                details.LastPhotoURL = string.Format("{0}://{1}:{2}/dzc/{3}/DZ/{4}.dzi", Request.Url.Scheme,
                                    Request.Url.Host,
                                    Request.Url.Port,
                                    site.ContainerID,
                                    ctx.Photos.Find(reader.GetInt64(3)).BlobID);
                            }
                        }
                    }
                }
            }


            return PartialView("_SiteDetails", details);
        }

    }
}
