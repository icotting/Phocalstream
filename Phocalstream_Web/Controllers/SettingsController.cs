using Microsoft.Practices.Unity;
using Phocalstream_Shared;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Web.Application.Admin;
using Phocalstream_Web.Application.Data;
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
        [Dependency]
        public IEntityRepository<User> UserRepository { get; set; }

        [Dependency]
        public IUnitOfWork Unit { get; set; }

        //
        // GET: /Settings/

        public ActionResult Index()
        {
            SettingsViewModel model = new SettingsViewModel();
            model.DmProcess = getDMModel();
            model.WaterProcess = getWaterModel();

            List<User> users = UserRepository.AsQueryable().ToList<User>();
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
            User user = UserRepository.Find(id);
            if (user != null)
            {
                UserRepository.Delete(user);
                Unit.Commit();
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult AddAdmin(long id)
        {
            User user = UserRepository.Find(id);
            if (user != null)
            {
                Roles.AddUserToRole(user.GoogleID, "Admin");
            }
           
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult RemoveAdmin(long id)
        {
            User user = UserRepository.Find(id);
            if (user != null)
            {
                Roles.RemoveUserFromRole(user.GoogleID, "Admin");
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult StartDmImport(string type)
        {
            DmImportProc model = getDMModel();
            model.Running = true;

            DroughtMonitorImporter.getInstance().RunDMImport(type);
            
            return PartialView("_DmImportPartial", model);
        }

        [HttpGet]
        public ActionResult CheckDMImport()
        {
            return PartialView("_DmImportPartial", getDMModel());
        }

        private DmImportProc getDMModel()
        {
            DmImportProc model = new DmImportProc();
            DroughtMonitorImporter importer = DroughtMonitorImporter.getInstance();

            model.Running = importer.ImportRunning;
            model.StartDate = importer.FirstDate;
            model.EndDate = importer.LastDate;

            return model;
        }

        [HttpGet]
        public ActionResult StartWaterImport(string type)
        {
            WaterImportProc model = getWaterModel();
            model.Running = true;

            if (type.Equals("full"))
            {
                WaterDataImporter.getInstance().ResetAllWaterData();
            }
            else
            {
                WaterDataImporter.getInstance().UpdateWaterData();
            }

            return PartialView("_WaterImportPartial", model);
        }

        [HttpGet]
        public ActionResult CheckWaterImport()
        {
            return PartialView("_WaterImportPartial", getWaterModel());
        }

        private WaterImportProc getWaterModel()
        {
            WaterImportProc model = new WaterImportProc();
            WaterDataImporter importer = WaterDataImporter.getInstance();

            model.Running = importer.ImportRunning;
            model.EndDate = importer.LastDate;

            return model;
        }
    }
}
