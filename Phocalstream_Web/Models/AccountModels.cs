using Phocalstream_Shared.Models;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models
{
    public class RegisterUserModel
    {
        [Required]
        [Display(Name = "User name")]
        public string ProviderUserName { get; set; }
        public string ProviderData { get; set; }
        public User User { get; set; }
    }

    public class UserManageModel
    {
        public string Status { get; set; }
        public User User { get; set; }
    }
}