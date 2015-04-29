using System.Linq;
using Microsoft.Practices.Unity;
using Phocalstream_Service.Data;
using Phocalstream_Service.Service;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Service;
using Phocalstream_Web.Application.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Threading;
using Phocalstream_Web.Application;

namespace Phocalstream_PhotoProcessor
{
    class Program
    {

        private static string _path;
        private static bool _break;
        private static bool _forceCollectionBuild = false;

        private static IPhotoService _service;
        private static IUnitOfWork _unit;

        static void Main(string[] args)
        {
            IUnityContainer container = BuildUnityContainer();

            _service = container.Resolve<IPhotoService>();
            _unit = container.Resolve<IUnitOfWork>();

            _path = PathManager.GetRawPath();

            if (args.Contains<string>(@"rebuild"))
            {
                _forceCollectionBuild = true;
            }

            Thread t = new Thread(new ThreadStart(BeginProcess));
            t.Start();

            Console.WriteLine("Press any key to terminate the import");
            Console.ReadKey();
            _break = true;
        }

        private static void BeginProcess()
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(Path.Combine(_path, @"Phocalstream_Manifest.xml"));

            XmlNodeList siteList = xml.SelectNodes("/SiteList/Site");

            foreach (XmlNode siteNode in siteList)
            {
                    string dirName = siteNode["Folder"].InnerText;
                    string[] files = Directory.GetFiles(Path.Combine(_path, dirName), "*.JPG", SearchOption.AllDirectories);
                    files = files.Select(f => f.Replace(_path, "")).ToArray<string>();
                    files = files.Select(f => f.Replace(@"\\", @"\")).ToArray<string>();

                    List<string> siteFiles = new List<string>();

                    using (SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand command = new SqlCommand("select FileName from Photos inner join CameraSites on CameraSites.ID = Photos.Site_ID where CameraSites.Name = @name", conn))
                        {
                            command.Parameters.AddWithValue("@name", dirName);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    siteFiles.Add(reader.GetString(0));
                                }
                            }
                        }
                    }

                    IEnumerable<string> toProcess = files.Except(siteFiles);

                    siteFiles = new List<string>();
                    files = new string[0];

                    if (toProcess.Count() != 0 || _forceCollectionBuild) 
                    {
                        Collection collection = _service.GetCollectionForProcessing(siteNode);
                        CameraSite site = collection.Site;
                        _unit.Commit();

                        int len = toProcess.Count();
                        Console.WriteLine(string.Format("Processing {0} photos for site {1}", len, site.Name));

                        int index = 0;
                        foreach (string file in toProcess)
                        {
                            Console.Write("\rFile {0} of {1}", index++, len);

                            try
                            {
                                _service.ProcessPhoto(file, site);
                            }
                            catch ( Exception e )
                            {
                                Console.WriteLine("Skipping image, error: {0}", e.Message);
                            }

                            if (index % 500 == 0)
                            {
                                _unit.Commit();
                            }

                            if (_break)
                                break;
                        }
                        Console.WriteLine("");
                        _unit.Commit();

                        if (_break)
                        {
                            break;
                        }

                        _service.FinishCollectionProcessing(collection);
                        _unit.Commit();
                    }
            }
            Console.WriteLine("Import process complete");
        }

        private static IUnityContainer BuildUnityContainer()
        {
            var container = new UnityContainer();
            container.RegisterType(typeof(IUnitOfWork), typeof(UnitOfWork));

            container.RegisterType(typeof(IDroughtMonitorRepository), typeof(DroughtMonitorRepository),
                new InjectionConstructor(@""));

            container.RegisterType(typeof(IWaterDataRepository), typeof(WaterDataRepository),
                new InjectionConstructor(@""));

            container.RegisterType(typeof(IPhotoService), typeof(PhotoService));
            container.RegisterType(typeof(IEntityRepository<>), typeof(EntityRepository<>));

            container.RegisterType(typeof(IPhotoRepository), typeof(PhotoRepository),
                new InjectionConstructor(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString));

            container.RegisterType(typeof(DbContext), typeof(ApplicationContext));

            container.RegisterInstance(new ApplicationContextAdapter(container.Resolve<DbContext>()), new HierarchicalLifetimeManager());
            container.RegisterType<IDbSetFactory>(new InjectionFactory(con => con.Resolve<ApplicationContextAdapter>()));
            container.RegisterType<IDbContext>(new InjectionFactory(con => con.Resolve<ApplicationContextAdapter>()));

            return container;
        }
    }
}