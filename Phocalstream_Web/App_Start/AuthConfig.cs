using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.WebPages.OAuth;
using System.Configuration;
using Phocalstream_Web.Application;

namespace Phocalstream_Web
{
    public static class AuthConfig
    {
        public static void RegisterAuth()
        {
            OAuthWebSecurity.RegisterFacebookClient(ConfigurationManager.AppSettings["facebookAppID"], ConfigurationManager.AppSettings["facebookAppSecret"]);
        }
    }
}
