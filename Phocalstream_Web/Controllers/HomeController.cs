using Phocalstream_Shared;
using Phocalstream_Shared.Models;
using Phocalstream_Web.Application;
using Phocalstream_Web.Models;
using Phocalstream_Web.Models;
using Phocalstream_Web.Models.ViewModels;
using System;
using System.Collections.Generic;
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

        [AllowAnonymous]
        public ActionResult Index()
        {
            HomeViewModel model = new HomeViewModel();

            using (EntityContext ctx = new EntityContext())
            {
                model.Collections = ctx.Collections.Include("Site").Where(c => c.Status == CollectionStatus.COMPLETE).ToList<Collection>();
                return View(model);
            }
        }
    }
}
