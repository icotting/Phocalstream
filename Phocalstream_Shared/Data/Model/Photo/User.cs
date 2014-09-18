using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data.Model.Photo
{
    public class User
    {
        [Key]
        public int ID { get; set; }
        public string ProviderID { get; set; }

        [Display(Name = "First name")]
        [Required]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        [Required]
        public string LastName { get; set; }

        public UserRole Role { get; set; }

        [Display(Name = "Preferred email address")]
        [Required]
        public string EmailAddress { get; set; }

        [Display(Name = "Affiliated organization or school")]
        public string Organization { get; set; }
    }
}
