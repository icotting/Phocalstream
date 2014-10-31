using Phocalstream_TimeLapseService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimelapseTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ITimeLapseManager manager = (ITimeLapseManager)Activator.GetObject(typeof(ITimeLapseManager), "tcp://localhost:8084/TimeLapseManager");
            List<long> ids = new List<long>();
            for (int i = 45; i <= 77; ++i)
            {
                ids.Add(i);
            }
            long job = manager.StartJob(ids, 20);
            Console.WriteLine(manager.GetJobDestination(job));
            Console.ReadKey();
        }
    }
}
