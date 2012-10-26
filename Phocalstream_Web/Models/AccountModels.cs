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

    public class User
    {
        [Key]
        public int ID { get; set; }
        public string GoogleID { get; set; }

        [Display(Name = "First name")]
        [Required]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        [Required]
        public string LastName { get; set; }

        public UserRole Role { get; set; }

        [Display(Name = "Preferred email address")]
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [Display(Name = "Affiliated organization or school")]
        public string Organization { get; set; }
    }

    public enum UserRole
    {
        ADMIN,
        REVIEWER,
        STANDARD
    }
}