using Phocalstream_Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class SettingsViewModel
    {
        public List<ManagedUser> UserList { get; set; }
    }

    public class ManagedUser
    {
        public User User { get; set; }
        public bool isAdmin { get; set; }
        public bool isCurrentUser { get; set; }
    }
}