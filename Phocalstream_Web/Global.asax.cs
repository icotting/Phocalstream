using Phocalstream_Web.Application;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
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

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            RouteTable.Routes.MapHttpRoute(name: "DZC Controller", routeTemplate: "api/dzc/{resource}", defaults: new { controller = "DzcController" });

            AuthConfig.RegisterAuth();
            WebSecurity.InitializeDatabaseConnection("DbConnection", "Users", "ID", "GoogleID", true);
        }
    }
}