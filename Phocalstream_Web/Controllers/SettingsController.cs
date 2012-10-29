using Phocalstream_Shared;
using Phocalstream_Shared.Models;
using Phocalstream_Web.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Phocalstream_Web.Controllers
{
    [Authorize(Roles=@"Admin")] 
    public class SettingsController : Controller
    {
        //
        // GET: /Settings/

        public ActionResult Index()
        {
            SettingsViewModel model = new SettingsViewModel();
            List<User> users;
            using (EntityContext ctx = new EntityContext())
            {
                users = ctx.Users.ToList<User>();
            }
            model.UserList = new List<ManagedUser>();
            
            foreach (var user in users)
            {
                model.UserList.Add(new ManagedUser() { User = user, 
                    isAdmin = Roles.IsUserInRole(user.GoogleID, "Admin"),
                    isCurrentUser = (this.User.Identity.Name == user.GoogleID)
                });
            }
            return View(model);
        }

        [HttpGet]
        public ActionResult Delete(long id)
        {
            using (EntityContext ctx = new EntityContext())
            {
                User user = ctx.Users.Find(id);
                if (user != null)
                {
                    ctx.Entry<User>(user).State = System.Data.EntityState.Deleted;
                    ctx.SaveChanges();
                }
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult AddAdmin(long id)
        {
            using (EntityContext ctx = new EntityContext())
            {
                User user = ctx.Users.Find(id);
                if (user != null)
                {
                    Roles.AddUserToRole(user.GoogleID, "Admin");
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult RemoveAdmin(long id)
        {
            using (EntityContext ctx = new EntityContext())
            {
                User user = ctx.Users.Find(id);
                if (user != null)
                {
                    Roles.RemoveUserFromRole(user.GoogleID, "Admin");
                }
            }

            return RedirectToAction("Index");
        }

    }
}
