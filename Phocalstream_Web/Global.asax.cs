using Microsoft.Practices.ServiceLocation;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Web.Application;
using Phocalstream_Web.Application.Admin;
using Phocalstream_Web.Application.Data;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using WebMatrix.WebData;

namespace Phocalstream_Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            GlobalConfiguration.Configuration.Filters.Add(new ExceptionFilter());

            GlobalConfiguration.Configure(WebApiConfig.Register);
            
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);

            UnityConfig.RegisterComponents();

            RouteConfig.RegisterRoutes(RouteTable.Routes);

            AuthConfig.RegisterAuth();
            WebSecurity.InitializeDatabaseConnection("DbConnection", "Users", "ID", "ProviderID", true);

            Scheduler.getInstance().AddJobToSchedule(new DmImporterJob());
            Scheduler.getInstance().AddJobToSchedule(new WaterImporterJob());
        }

        /* Handler for authentication from the mobile app using Facebook */
        protected void FormsAuthentication_OnAuthenticate(Object sender, FormsAuthenticationEventArgs e)
        {
            if (FormsAuthentication.CookiesSupported == true)
            {
                if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
                {
                    string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
                    IEntityRepository<User> userRepository = ServiceLocator.Current.GetInstance<IEntityRepository<User>>();

                    User user = userRepository.Find(u => u.ProviderID == username).FirstOrDefault();
                    if  (user == null)
                    {
                        FormsAuthentication.SignOut();
                    }
                    else
                    {
                        MobileIdentityPrincipal principal = new MobileIdentityPrincipal(user);
                        HttpContext.Current.User = principal;
                        System.Threading.Thread.CurrentPrincipal = principal;

                    }
                }
            }
        }
    }
}