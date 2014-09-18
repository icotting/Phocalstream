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
            // To let users of this site log in using their accounts from other sites such as Microsoft, Facebook, and Twitter,
            // you must update this site. For more information visit http://go.microsoft.com/fwlink/?LinkID=252166

            //OAuthWebSecurity.RegisterMicrosoftClient(
            //    clientId: "",
            //    clientSecret: "");

            //OAuthWebSecurity.RegisterTwitterClient(
            //    consumerKey: "",
            //    consumerSecret: "");

            OAuthWebSecurity.RegisterClient(new FacebookIdentityClient(ConfigurationManager.AppSettings["facebookAppID"], ConfigurationManager.AppSettings["facebookAppSecret"], 
                "email,user_likes,friends_likes,user_birthday,publish_checkins,publish_stream"), "Facebook", null);

            //OAuthWebSecurity.RegisterGoogleClient();
        }
    }
}
