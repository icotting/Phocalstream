using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace Phocalstream_Web.Models
{
    public class MobileIdentityPrincipal : GenericPrincipal
    {
        public MobileIdentityPrincipal(IIdentity genericIdentity, string[] p) : base(genericIdentity, p) { }

        public MobileIdentityPrincipal(User identity)
            : base(new GenericIdentity(identity.ProviderID), new string[] { identity.Role == UserRole.ADMIN ? "Admin" : "User" })
        {
            this.Identity = identity;
        }

        public User Identity { get; set; }
    }
}