using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;
using Phocalstream_Web.Application;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebMatrix.WebData;

namespace Phocalstream_Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public ActionResult UserProfile()
        {
            User user;
            using (EntityContext ctx = new EntityContext())
            {
                user = ctx.Users.Where(u => u.GoogleID == this.User.Identity.Name).FirstOrDefault<User>();
            }

            return View(new UserManageModel() { User = user });
        }

        [AllowAnonymous]
        public ActionResult LoginFailure()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            WebSecurity.Logout();

            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        public ActionResult UserProfile(UserManageModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (EntityContext ctx = new EntityContext())
                    {
                        ctx.Entry<User>(model.User).State = System.Data.EntityState.Modified;
                        ctx.SaveChanges();
                    }
                    model.Status = "Profile updated";
                }
                catch (Exception e)
                {
                    model.Status = String.Format("An error occurred: {0}", e.Message);
                }
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            return new ExternalLoginResult("Google", Url.Action("LoginCallback", new { ReturnUrl = returnUrl }));
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login()
        {
            return new ExternalLoginResult("Google", Url.Action("LoginCallback", new { ReturnUrl = "~/" }));
        }

        [AllowAnonymous]
        public ActionResult LoginCallback(string returnUrl)
        {
            AuthenticationResult result = OAuthWebSecurity.VerifyAuthentication(Url.Action("LoginCallback", new { ReturnUrl = returnUrl }));
            if (!result.IsSuccessful)
            {
                return RedirectToAction("LoginFailure");
            }

            if (OAuthWebSecurity.Login(result.Provider, result.ProviderUserId, createPersistentCookie: false))
            {
                return RedirectToLocal(returnUrl);
            }

            if (User.Identity.IsAuthenticated)
            {
                // If the current user is logged in add the new account
                OAuthWebSecurity.CreateOrUpdateAccount(result.Provider, result.ProviderUserId, User.Identity.Name);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // User is new, ask for their desired membership name
                string loginData = OAuthWebSecurity.SerializeProviderUserId(result.Provider, result.ProviderUserId);
                ViewBag.ReturnUrl = returnUrl;
                return View("RegisterAccount", new RegisterUserModel { ProviderUserName = result.UserName, ProviderData = loginData, User = new Models.User()});
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterAccount(RegisterUserModel model, string returnUrl)
        {
            string provider = null;
            string providerUserId = null;

            if (User.Identity.IsAuthenticated || !OAuthWebSecurity.TryDeserializeProviderUserId(model.ProviderData, out provider, out providerUserId))
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                // Insert a new user into the database
                using (EntityContext ctx = new EntityContext())
                {
                    Phocalstream_Web.Models.User user = ctx.Users.FirstOrDefault(u => u.GoogleID == model.ProviderUserName);

                    // Check if user already exists
                    if (user == null)
                    {
                        // Insert name into the profile table
                        model.User.GoogleID = model.ProviderUserName;
                        model.User.Role = UserRole.STANDARD;
                        ctx.Users.Add(model.User);
                        ctx.SaveChanges();

                        OAuthWebSecurity.CreateOrUpdateAccount(provider, providerUserId, model.ProviderUserName);
                        OAuthWebSecurity.Login(provider, providerUserId, createPersistentCookie: false);

                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        ModelState.AddModelError("UserName", "User name already exists. Please enter a different user name.");
                    }
                }
            }
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }


        #region Helpers
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
        #endregion

        internal class ExternalLoginResult : ActionResult
        {
            public ExternalLoginResult(string provider, string returnUrl)
            {
                Provider = provider;
                ReturnUrl = returnUrl;
            }

            public string Provider { get; private set; }
            public string ReturnUrl { get; private set; }

            public override void ExecuteResult(ControllerContext context)
            {
                OAuthWebSecurity.RequestAuthentication(Provider, ReturnUrl);
            }
        }
    }
}
