using Microsoft.Practices.Unity;
using Microsoft.Web.WebPages.OAuth;
using Newtonsoft.Json;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Security;

namespace Phocalstream_Web.Controllers.Api
{
    public class MobileClientController : ApiController
    {
        [Dependency]
        public IEntityRepository<User> UserRepository { get; set; }

        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage Authenticate(string fbToken)
        {

            Dictionary<string, string> values = null;
            using (var client = new WebClient())
            {
                string url = string.Format("https://graph.facebook.com/me?access_token={0}", fbToken);
                try
                {
                    var json = client.DownloadString(url);
                    values = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                }                    
                catch (Exception e)
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
            }

            if ( values.ContainsKey("email") && values.ContainsKey("id") )
            {
                string email = values["email"];
                string fbID = values["id"];

                var user = UserRepository.Find(u => u.ProviderID == email).FirstOrDefault();
                if ( user != null )
                {
                    OAuthWebSecurity.Login("facebook", fbID, false);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.Forbidden);
                }
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
        }
    }
}