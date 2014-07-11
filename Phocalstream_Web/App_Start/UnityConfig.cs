using System.Web.Mvc;
using Microsoft.Practices.Unity;
using Unity.Mvc5;
using Phocalstream_Shared.Data;
using Phocalstream_Service.Data;
using System.Configuration;
using Phocalstream_Web.Application.Data;
using Phocalstream_Shared.Service;
using Phocalstream_Service.Service;
using System.Data.Entity;
using Phocalstream_Web.Application.Admin;
using System.Web.Http;

namespace Phocalstream_Web
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
			var container = new UnityContainer();

            container.RegisterType(typeof(IDroughtMonitorRepository), typeof(DroughtMonitorRepository), 
                new InjectionConstructor(ConfigurationManager.ConnectionStrings["DMConnection"].ConnectionString));

            container.RegisterType(typeof(IWaterDataRepository), typeof(WaterDataRepository),
                new InjectionConstructor(ConfigurationManager.ConnectionStrings["WaterDBConnection"].ConnectionString));

            container.RegisterType(typeof(IPhotoRepository), typeof(PhotoRepository),
                new InjectionConstructor(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString));

            container.RegisterType(typeof(IPhotoService), typeof(PhotoService));
            container.RegisterType(typeof(IUnitOfWork), typeof(UnitOfWork));
            container.RegisterType(typeof(IEntityRepository<>), typeof(EntityRepository<>));
            container.RegisterType(typeof(DbContext), typeof(ApplicationContext));

            DroughtMonitorImporter.InitWithContainer(container);
            WaterDataImporter.InitWithContainer(container);

            container.RegisterInstance(new ApplicationContextAdapter(container.Resolve<DbContext>()), new HierarchicalLifetimeManager());
            container.RegisterType<IDbSetFactory>(new InjectionFactory(con => con.Resolve<ApplicationContextAdapter>()));
            container.RegisterType<IDbContext>(new InjectionFactory(con => con.Resolve<ApplicationContextAdapter>()));

            DependencyResolver.SetResolver(new Unity.Mvc5.UnityDependencyResolver(container));

            GlobalConfiguration.Configuration.DependencyResolver = new Unity.WebApi.UnityDependencyResolver(container);
        }
    }
}