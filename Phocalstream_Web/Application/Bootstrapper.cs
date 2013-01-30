using Microsoft.Practices.Unity;
using Phocalstream_Shared.Data;
using Phocalstream_Web.Application.Data;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Data.Entity;

namespace Phocalstream_Web.Application
{
    public class Bootstrapper
    {
        public static void Initialise()
        {
            var container = BuildUnityContainer();
            DependencyResolver.SetResolver(new Unity.Mvc3.UnityDependencyResolver(container));
            GlobalConfiguration.Configuration.DependencyResolver = new Unity.WebApi.UnityDependencyResolver(container);
        }

        private static IUnityContainer BuildUnityContainer()
        {
            var container = new UnityContainer();
            container.RegisterType(typeof(IDroughtMonitorRepository), typeof(DroughtMonitorRepository));
            container.RegisterType(typeof(IWaterDataRepository), typeof(WaterDataRepository));
            container.RegisterType(typeof(IPhotoRepository), typeof(PhotoRepository));

            container.RegisterType(typeof(IEntityRepository<>), typeof(EntityRepository<>));
            container.RegisterType(typeof(DbContext), typeof(ApplicationContext));

            container.RegisterInstance(new ApplicationContextAdapter(container.Resolve<DbContext>()), new ContainerControlledLifetimeManager());
            container.RegisterType<IDbSetFactory>(new InjectionFactory(con => con.Resolve<ApplicationContextAdapter>()));
            container.RegisterType<IDbContext>(new InjectionFactory(con => con.Resolve<ApplicationContextAdapter>()));

            return container;
        }
    }
}