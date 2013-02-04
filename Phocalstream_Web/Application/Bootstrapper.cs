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
using System.Configuration;
using Phocalstream_Web.Application.Admin;

namespace Phocalstream_Web.Application
{
    public class Bootstrapper
    {
        public static void Initialise()
        {
            var container = BuildUnityContainer();

            /* initialize the external data importers with the appropriately injected repositories */
            DroughtMonitorImporter.InitWithContainer(container);
            WaterDataImporter.InitWithContainer(container);

            DependencyResolver.SetResolver(new Unity.Mvc3.UnityDependencyResolver(container));
            GlobalConfiguration.Configuration.DependencyResolver = new Unity.WebApi.UnityDependencyResolver(container);
        }

        private static IUnityContainer BuildUnityContainer()
        {
            var container = new UnityContainer();
            container.RegisterType(typeof(IDroughtMonitorRepository), typeof(DroughtMonitorRepository),
                new InjectionConstructor(ConfigurationManager.ConnectionStrings["DMConnection"].ConnectionString));

            container.RegisterType(typeof(IWaterDataRepository), typeof(WaterDataRepository),
                new InjectionConstructor(ConfigurationManager.ConnectionStrings["WaterDBConnection"].ConnectionString));

            container.RegisterType(typeof(IPhotoRepository), typeof(PhotoRepository), 
                new InjectionConstructor(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString));

            container.RegisterType(typeof(IUnitOfWork), typeof(UnitOfWork));
            container.RegisterType(typeof(IEntityRepository<>), typeof(EntityRepository<>));
            container.RegisterType(typeof(DbContext), typeof(ApplicationContext));

            container.RegisterInstance(new ApplicationContextAdapter(container.Resolve<DbContext>()), new HierarchicalLifetimeManager());
            container.RegisterType<IDbSetFactory>(new InjectionFactory(con => con.Resolve<ApplicationContextAdapter>()));
            container.RegisterType<IDbContext>(new InjectionFactory(con => con.Resolve<ApplicationContextAdapter>()));

            return container;
        }
    }
}