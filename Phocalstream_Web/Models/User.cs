using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models
{
    public class User
    {
        public long ID { get; set; }
        public string GoogleID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}