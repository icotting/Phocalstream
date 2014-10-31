using System;
using System.Collections.Generic;
using System.Configuration;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_TimeLapseService
{
	public class TimeLapseService : ServiceBase
	{
		public static void Main(string[] args)
		{
            if (Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                }
            }
#if(!DEBUG) 
                ServiceBase.Run(new TimeLapseService());
#else
            TimeLapseService service = new TimeLapseService();

            Console.WriteLine("Starting service...");
            service.OnStart(args);
            Console.WriteLine("Service is running.");
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey(true);
            Console.WriteLine("Stopping service...");
            service.OnStop();
            Console.WriteLine("Service stopped.");
#endif
        }

		protected override void OnStart(string[] args)
		{
            PathManager.ValidateTimelapsePaths();

			AppDomain.CurrentDomain.SetData("DataDirectory", "C:\\PhocalStream\\Phocalstream\\Phocalstream_Web\\App_Data");
			System.IO.Directory.SetCurrentDirectory(PathManager.GetOutputPath());
			TcpChannel channel = new TcpChannel(8084);
			ChannelServices.RegisterChannel(channel, false);
			RemotingConfiguration.RegisterWellKnownServiceType(typeof(TimeLapseManager), "TimeLapseManager", WellKnownObjectMode.Singleton);
			ITimeLapseManager manager = (ITimeLapseManager)Activator.GetObject(typeof(ITimeLapseManager), "tcp://localhost:8084/TimeLapseManager");
			manager.ImportJobs(PathManager.GetOutputPath() + "/jobs.ini");
		}

		protected override void OnStop()
		{
			ITimeLapseManager manager = (ITimeLapseManager)Activator.GetObject(typeof(ITimeLapseManager), "tcp://localhost:8084/TimeLapseManager");
			manager.ExportJobs(PathManager.GetOutputPath() + "/jobs.ini");
		}

		private void InitializeComponent()
		{

		}
	}
}
