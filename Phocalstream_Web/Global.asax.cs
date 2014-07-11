using Phocalstream_Web.Application;
using Phocalstream_Web.Application.Admin;
using Phocalstream_Web.Application.Data;
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

            GlobalConfiguration.Configuration.Filters.Add(new ExceptionFilter());

            GlobalConfiguration.Configure(WebApiConfig.Register);
            
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);

            UnityConfig.RegisterComponents();

            RouteConfig.RegisterRoutes(RouteTable.Routes);

            AuthConfig.RegisterAuth();
            WebSecurity.InitializeDatabaseConnection("DbConnection", "Users", "ID", "GoogleID", true);

            Scheduler.getInstance().AddJobToSchedule(new DmImporterJob());
            Scheduler.getInstance().AddJobToSchedule(new WaterImporterJob());
        }
    }
}