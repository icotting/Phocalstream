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
using Phocalstream_Shared.Model.View;

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
            return new ExternalLoginResult("Facebook", Url.Action("LoginCallback"));
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login()
        {
            return new ExternalLoginResult("Facebook", Url.Action("LoginCallback"));
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

                string email = "";
                string name = "";
                result.ExtraData.TryGetValue("username", out email);
                result.ExtraData.TryGetValue("name", out name);

                User tempUser = new User();
                tempUser.EmailAddress = email;
                
                string[] names = name.Split(' ');
                if (names.Count() > 0)
                {
                    tempUser.FirstName = names[0];
                }
                if (names.Count() > 1)
                {
                    tempUser.LastName = names[1];
                }

                return View("RegisterAccount", new RegisterUserModel { ProviderUserName = result.UserName, ProviderData = loginData, User = tempUser});
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
                        if (!Roles.RoleExists("Admin"))
                        {
                            Roles.CreateRole("Admin");
                        }
                        Roles.AddUserToRole(model.ProviderUserName, "Admin");
                    }

                    //Create a favorites collect for the new user
                    CollectionService.NewUserCollection(model.User, "Favorites", "");

                    //Take the new user to their collections page
                    return RedirectToAction("UserCollections");
                }
                else
                {
                    ModelState.AddModelError("UserName", "User name already exists. Please enter a different user name.");
                }
            }
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        public ActionResult UserCollections(int e = 0)
        {
            UserCollectionList model = new UserCollectionList();

            Phocalstream_Shared.Data.Model.Photo.User User = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            model.User = User;

            model.SiteThumbnails = new List<ThumbnailModel>();
            model.TimelapseThumbnails = new List<ThumbnailModel>();
            model.CollectionThumbnails = new List<ThumbnailModel>();

            model.Collections = CollectionRepository.Find(c => c.Owner.ID == User.ID, c => c.Photos).ToList<Collection>();
            foreach (var col in model.Collections)
            {
                if (col.CoverPhoto == null)
                {
                    col.CoverPhoto = col.Photos.LastOrDefault();
                }

                var thumb = new ThumbnailModel()
                {
                    ID = col.ID,
                    Name = col.Name,
                    PhotoCount = col.Photos.Count,
                    Link = "/search/index?collectionId=" + col.ID.ToString(),

                    CanEdit = true,
                    EditLink = "/Account/EditUserCollection?collectionID=" + col.ID.ToString(),
                    CanDelete = true,
                    DeleteLink = "/Account/DeleteUserCollection?collectionID=" + col.ID.ToString()
                };

                if (thumb.PhotoCount > 0)
                {
                    thumb.First = col.Photos.First().Captured;
                    thumb.Last = col.Photos.Last().Captured;
                    thumb.CoverPhotoID = col.CoverPhoto.ID;
                }

                switch (col.Type)
                {
                    case CollectionType.TIMELAPSE:
                        model.TimelapseThumbnails.Add(thumb);
                        break;
                    case CollectionType.USER:
                        if (col.Site == null)
                        {
                            model.CollectionThumbnails.Add(thumb);
                        }
                        else
                        {
                            model.SiteThumbnails.Add(thumb);
                        }
                        break;
                }
            }

            if (e == 1)
            {
                ViewBag.Message = "That collection doesn't contain any photos.";
            }
            else if (e == 2)
            {
                ViewBag.Message = "Successfully deleted collection.";
            }
            else if (e == 3)
            {
                ViewBag.Message = "Error deleting collection.";
            }

            return View(model);
        }
        
        public ActionResult UploadPhotos(long collectionID = 0)
        {
            User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            UserPhotoUpload model = new UserPhotoUpload();
            model.UserSiteCollections = CollectionRepository.Find(c => c.Type == CollectionType.USER && c.Owner.ID == user.ID && c.Site != null, c => c.Site).ToList();

            if (model.UserSiteCollections.Count == 0)
            {
                return RedirectToAction("CreateUserSite", new { e = 1 });
            }

            ViewBag.CollectionID = collectionID;

            return View(model);
        }

        public ActionResult CreateUserSite(int e = 0)
        {
            if (e == 1)
            {
                ViewBag.Message = "You must create a photo site before you can upload photos. <strong>Where were these photos taken?</strong>";
            }

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

            return RedirectToAction("UploadPhotos", new { @collectionID = newCollection.ID });
        }

        public ActionResult DeleteUserCollection(long collectionID)
        {
            Phocalstream_Shared.Data.Model.Photo.User User = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            if (User != null)
            {
                if (CollectionService.DeleteUserCollection(User.ID, collectionID))
                {
                    return RedirectToAction("UserCollections", "Account", new { e = 2 }); // success message
                }
            }

            return RedirectToAction("UserCollections", "Account", new { e = 3 }); // error message
        }

        public ActionResult DeleteUserCollections()
        {
            CollectionService.DeleteUserCollections(UserRepository.First(u => u.ProviderID == this.User.Identity.Name).ID);
            return RedirectToAction("UserCollections", "Account");
        }

        public ActionResult EditUserCollection(long collectionID, int count = -1)
        {
            EditUserCollection model = new EditUserCollection();
            model.Collection = CollectionRepository.First(c => c.ID == collectionID, c => c.Photos);
            model.CoverPhotoId = model.Collection.CoverPhoto != null ? model.Collection.CoverPhoto.ID : 0;

            if (count != -1)
            {
                if (count == 1)
                {
                    ViewBag.Message = "Successfully deleted 1 photo.";
                }
                else
                {
                    ViewBag.Message = string.Format("Successfully deleted {0} photos.",  count.ToString());
                }
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
