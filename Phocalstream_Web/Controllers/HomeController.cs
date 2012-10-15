using Phocalstream_Web.Application;
using Phocalstream_Web.Models;
using Phocalstream_Web.Models.Entity;
using Phocalstream_Web.Models.ViewModels;
using System;
using System.Collections.Generic;
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
            CollectionViewModel model = new CollectionViewModel();

            using (EntityContext ctx = new EntityContext())
            {
                model.Collections = (from c in ctx.Collections select c).ToList<Collection>();
            }

            return View(model);
        }

    }
}
