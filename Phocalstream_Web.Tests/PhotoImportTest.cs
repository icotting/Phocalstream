using Microsoft.Practices.Unity;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phocalstream_Shared.Service;
using Phocalstream_Shared.Data;
using Phocalstream_Service.Service;
using Phocalstream_Shared.Data.Model.Photo;
using System.Configuration;
using Phocalstream_Service.Data;
using Phocalstream_Web.Application.Data;
using System.Data.Entity;
using Phocalstream_Web.Application.Admin;
using System.Web.Mvc;
using Unity.Mvc5;

namespace Phocalstream_Web.Tests
{
    [TestClass]
    public class PhotoImportTest
    {
        private IUnityContainer container;

        [Dependency]
        public IPhotoService PhotoService { get; set; }

        [TestInitialize()]
        public void MyTestInitialize()
        {
            container = new UnityContainer();
            
            container.RegisterType(typeof(IDroughtMonitorRepository), typeof(DroughtMonitorRepository),
                  new InjectionConstructor(ConfigurationManager.ConnectionStrings["DMConnection"].ConnectionString));

            container.RegisterType(typeof(IWaterDataRepository), typeof(WaterDataRepository),
                new InjectionConstructor(ConfigurationManager.ConnectionStrings["WaterDBConnection"].ConnectionString));

            container.RegisterType(typeof(IPhotoRepository), typeof(PhotoRepository),
                new InjectionConstructor(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString));

            container.RegisterType(typeof(IPhotoService), typeof(PhotoService));
            container.RegisterType(typeof(ISearchService), typeof(SearchService));
            container.RegisterType(typeof(ICollectionService), typeof(CollectionService));
            container.RegisterType(typeof(IUnitOfWork), typeof(UnitOfWork));
            container.RegisterType(typeof(IEntityRepository<>), typeof(EntityRepository<>));
            container.RegisterType(typeof(DbContext), typeof(ApplicationContext));

            DroughtMonitorImporter.InitWithContainer(container);
            WaterDataImporter.InitWithContainer(container);

            container.RegisterInstance(new ApplicationContextAdapter(container.Resolve<DbContext>()), new HierarchicalLifetimeManager());
            container.RegisterType<IDbSetFactory>(new InjectionFactory(con => con.Resolve<ApplicationContextAdapter>()));
            container.RegisterType<IDbContext>(new InjectionFactory(con => con.Resolve<ApplicationContextAdapter>()));

            DependencyResolver.SetResolver(new Unity.Mvc5.UnityDependencyResolver(container));

            // GlobalConfiguration.Configuration.DependencyResolver = new Unity.WebApi.UnityDependencyResolver(container);

            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
        }

        [TestMethod]
        public void CountRGBPixelTest()
        {
            var PhotoService = container.Resolve<IPhotoService>();
            var PhotoRepository = container.Resolve<IEntityRepository<Photo>>();

            var photo = PhotoService.ProcessRGBForExistingPhoto(1);
            
            Assert.IsNotNull(photo);

            var total = photo.Black + photo.White + photo.Red + photo.Green + photo.Blue;
            Assert.AreEqual(1, total);

            Console.WriteLine(string.Format("{0}: {1}", PixelColor.BLACK, photo.Black));
            Console.WriteLine(string.Format("{0}: {1}", PixelColor.WHITE, photo.White));
            Console.WriteLine(string.Format("{0}: {1}", PixelColor.RED, photo.Red));
            Console.WriteLine(string.Format("{0}: {1}", PixelColor.GREEN, photo.Green));
            Console.WriteLine(string.Format("{0}: {1}", PixelColor.BLUE, photo.Blue));
        }
    }
}
