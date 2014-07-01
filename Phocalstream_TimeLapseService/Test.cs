using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Phocalstream_TimeLapseService
{
	public class Test
	{
		/*static void Main(string[] args)
		{
			ITimeLapseManager manager = (ITimeLapseManager)Activator.GetObject(typeof(ITimeLapseManager), "tcp://localhost:8084/TimeLapseManager");
			List<long> ids = new List<long>();
			for(int i = 45; i <= 77; ++i)
			{
				ids.Add(i);
			}
			long job = manager.StartJob(ids, 20);
			Console.WriteLine(manager.GetJobDestination(job));
			Console.ReadKey();
		}*/
	}
}
