using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;
using Phocalstream_Shared;
using Phocalstream_Web.Application;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebMatrix.WebData;
using System.Web.Security;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Web.Application.Data;
using Phocalstream_Web.Models;
using Microsoft.Practices.Unity;
using System.Data.Entity;
using Phocalstream_Shared.Data;
using Phocalstream_Web.Models.ViewModels;
using Phocalstream_Shared.Service;
using Phocalstream_Shared.Data.Model.View;

namespace Phocalstream_Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        [Dependency]
        public IEntityRepository<User> UserRepository { get; set; }

        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IDroughtMonitorRepository DroughtMonitorRepository { get; set; }

        [Dependency]
        public IUnitOfWork Unit { get; set; }

        [Dependency]
        public ICollectionService CollectionService { get; set; }

        public ActionResult UserProfile()
        {
            User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);            
            return View(new UserManageModel() { User = user });
        }

        [AllowAnonymous]
        public ActionResult LoginFailure()
        {
            return View();
        }

        [HttpGet]
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
                    UserRepository.Update(model.User);
                    Unit.Commit();
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
            return new ExternalLoginResult("Facebook", Url.Action("LoginCallback", new { ReturnUrl = returnUrl }));
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login()
        {
            return new ExternalLoginResult("Facebook", Url.Action("LoginCallback", new { ReturnUrl = "~/" }));
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
                return View("RegisterAccount", new RegisterUserModel { ProviderUserName = result.UserName, ProviderData = loginData, User = new Phocalstream_Shared.Data.Model.Photo.User()});
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
                User user = UserRepository.First(u => u.ProviderID == model.ProviderUserName);

                // Check if user already exists
                if (user == null)
                {
                    // Insert name into the profile table
                    model.User.ProviderID = model.ProviderUserName;
                    model.User.Role = UserRole.STANDARD;
                    UserRepository.Insert(model.User);
                    Unit.Commit();

                    OAuthWebSecurity.CreateOrUpdateAccount(provider, providerUserId, model.ProviderUserName);
                    OAuthWebSecurity.Login(provider, providerUserId, createPersistentCookie: false);

                    if (UserRepository.AsQueryable().Count() == 1)
                    {
                        Roles.CreateRole("Admin");
                        Roles.AddUserToRole(model.ProviderUserName, "Admin");
                    }

                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError("UserName", "User name already exists. Please enter a different user name.");
                }
            }
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        public ActionResult UserCollections()
        {
            UserCollectionList model = new UserCollectionList();

            Phocalstream_Shared.Data.Model.Photo.User User = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            model.User = User;
            model.Collections = CollectionRepository.Find(c => c.Owner.ID == User.ID, c => c.Photos);

            foreach (var col in model.Collections)
            {
                col.CoverPhoto = col.Photos.LastOrDefault(); 
            }


            return View(model);
        }
        
        public ActionResult UploadPhotos()
        {
            UserPhotoUpload model = new UserPhotoUpload();

            model.UserSiteCollections = CollectionRepository.Find(c => c.Type == CollectionType.USER && c.Site != null, c => c.Site).ToList();

            if (model.UserSiteCollections.Count == 0)
            {
                return new RedirectResult("CreateUserSite");
            }

            return View(model);
        }

        public ActionResult CreateUserSite()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreateUserSite(AddUserCameraSite site)
        {
            User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            string guid = Guid.NewGuid().ToString();

            CameraSite newCameraSite = new CameraSite()
            {
                Name = site.CameraSiteName,
                Latitude = site.Latitude,
                Longitude = site.Longitude,
                CountyFips = DroughtMonitorRepository.GetFipsForCountyAndState(site.County, site.State),
                ContainerID = guid,
                DirectoryName = guid
            };

            Collection newCollection = new Collection()
            {
                Name = site.CameraSiteName,
                Site = newCameraSite,
                Owner = user,
                ContainerID = guid,
                Type = CollectionType.USER
            };

            CollectionRepository.Insert(newCollection);
            Unit.Commit();

            return new RedirectResult("UserCollections");
        }

        public ActionResult DeleteUserCollection(long collectionID)
        {
            CollectionService.DeleteUserCollection(collectionID);
            return RedirectToAction("UserCollections", "Account");
        }

        public ActionResult DeleteUserCollections()
        {
            CollectionService.DeleteUserCollections(UserRepository.First(u => u.ProviderID == this.User.Identity.Name).ID);
            return RedirectToAction("UserCollections", "Account");
        }

        public ActionResult EditUserCollection(long collectionID)
        {
            Collection collection = CollectionRepository.First(c => c.ID == collectionID, c => c.Photos);

            return View(collection);
        }

        public ActionResult UserDefinedCollection(long collectionID)
        {
            UserDefinedCollection model = new UserDefinedCollection();
            
            Collection collection = CollectionRepository.First(col => col.ID == collectionID, col => col.Photos);
            model.CollectionName = collection.Name;

            model.First = collection.Photos.First().Captured;
            model.Last = collection.Photos.Last().Captured;
            model.PhotoCount = collection.Photos.Count;

            model.CollectionUrl = string.Format("{0}://{1}:{2}/api/sitecollection/pivotcollectionfor?id={3}", Request.Url.Scheme,
                Request.Url.Host,
                Request.Url.Port,
                collection.ID);

            if (collection.Status == CollectionStatus.INVALID)
            {
                CollectionService.UpdateUserCollection(collection);
            }

            Phocalstream_Shared.Data.Model.Photo.User User = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            if (User != null)
            {
                UserCollectionList userCollectionModel = new UserCollectionList();
                userCollectionModel.User = User;
                userCollectionModel.Collections = CollectionRepository.Find(c => c.Owner.ID == User.ID && c.Type == CollectionType.USER, c => c.Photos).ToList();
                model.UserCollections = userCollectionModel;
            }

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
