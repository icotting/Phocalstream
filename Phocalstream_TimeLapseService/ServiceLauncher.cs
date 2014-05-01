using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
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
			ServiceBase.Run(new TimeLapseService());
		}

		protected override void OnStart(string[] args)
		{
			AppDomain.CurrentDomain.SetData("DataDirectory", "C:\\PhocalStream\\Phocalstream\\Phocalstream_Web\\App_Data");
			System.IO.Directory.SetCurrentDirectory(ConfigurationManager.AppSettings["outputPath"]);
			TcpChannel channel = new TcpChannel(8084);
			ChannelServices.RegisterChannel(channel, false);
			RemotingConfiguration.RegisterWellKnownServiceType(typeof(TimeLapseManager), "TimeLapseManager", WellKnownObjectMode.Singleton);
			ITimeLapseManager manager = (ITimeLapseManager)Activator.GetObject(typeof(ITimeLapseManager), "tcp://localhost:8084/TimeLapseManager");
			manager.ImportJobs(ConfigurationManager.AppSettings["outputPath"] + "/jobs.ini");
		}

		protected override void OnStop()
		{
			ITimeLapseManager manager = (ITimeLapseManager)Activator.GetObject(typeof(ITimeLapseManager), "tcp://localhost:8084/TimeLapseManager");
			manager.ExportJobs(ConfigurationManager.AppSettings["outputPath"] + "/jobs.ini");
		}

		private void InitializeComponent()
		{

		}
	}
}
