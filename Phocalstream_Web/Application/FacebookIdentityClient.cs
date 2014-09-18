using DotNetOpenAuth.AspNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Phocalstream_Web.Application
{
    public class FacebookIdentityClient : IAuthenticationClient
    {

        private string appId;
        private string appSecret;
        private string scope;

        private const string baseUrl = "https://www.facebook.com/dialog/oauth?client_id=";
        public const string graphApiToken = "https://graph.facebook.com/oauth/access_token?";
        public const string graphApiMe = "https://graph.facebook.com/me?";

        private static string GetHTML(string URL)
        {
            string connectionString = URL;

            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] token = client.DownloadData(connectionString);
                    return Encoding.UTF8.GetString(token);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not get the result HTML", e);
            }
        }

        private IDictionary<string, string> GetUserData(string accessCode, string redirectURI)
        {
            string token = GetHTML(graphApiToken + "client_id=" + appId + "&redirect_uri=" + HttpUtility.UrlEncode(redirectURI) + "&client_secret=" + appSecret + "&code=" + accessCode);
            if (token == null || token == "")
            {
                return null;
            }
            string access_token = token.Substring(token.IndexOf("access_token="), token.IndexOf("&"));
            string data = GetHTML(graphApiMe + "fields=id,name,email,gender,link&" + access_token);

            Dictionary<string, string> userData = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
            return userData;
        }

        public FacebookIdentityClient(string appId, string appSecret, string scope)
        {
            this.appId = appId;
            this.appSecret = appSecret;
            this.scope = scope;
        }

        public string ProviderName
        {
            get { return "Facebook"; }
        }

        public void RequestAuthentication(System.Web.HttpContextBase context, Uri returnUrl)
        {
            string url = baseUrl + appId + "&redirect_uri=" + HttpUtility.UrlEncode(returnUrl.ToString()) + "&scope=" + scope;
            context.Response.Redirect(url);
        }

        public AuthenticationResult VerifyAuthentication(System.Web.HttpContextBase context)
        {
            string code = context.Request.QueryString["code"];

            string rawUrl = context.Request.Url.OriginalString;
            //From this we need to remove code portion
            rawUrl = Regex.Replace(rawUrl, "&code=[^&]*", "");

            IDictionary<string, string> userData = GetUserData(code, rawUrl);

            if (userData == null)
                return new AuthenticationResult(false, ProviderName, null, null, null);

            string id = userData["id"];
            string username = userData["id"];
            userData.Remove("id");
            userData.Remove("id");

            AuthenticationResult result = new AuthenticationResult(true, ProviderName, id, username, userData);
            return result;
        }
    }
}