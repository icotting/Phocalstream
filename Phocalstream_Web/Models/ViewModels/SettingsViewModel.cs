using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class SettingsViewModel
    {
        public List<ManagedUser> UserList { get; set; }
        public DmImportProc DmProcess { get; set; }
        public WaterImportProc WaterProcess { get; set; }
    }

    public class ManagedUser
    {
        public User User { get; set; }
        public bool isAdmin { get; set; }
        public bool isCurrentUser { get; set; }
    }

    public class DmImportProc
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public bool Running { get; set; }
    }

    public class WaterImportProc
    {
        public string EndDate { get; set; }
        public bool Running { get; set; }
    }
}