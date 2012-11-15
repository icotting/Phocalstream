using Quartz;
using Quartz.Impl;
using System;
using System.Runtime.CompilerServices;
using System.Web;

namespace Phocalstream_Web.Application.Data
{
    public interface PhocalstreamJob : IJob
    {
        string GetSchedule();
    }

    public class Scheduler
    {
        private static Scheduler _instance;

        private IScheduler _sched;
        
        private Scheduler()
        {
            ISchedulerFactory schedFact = new StdSchedulerFactory();
            this._sched = schedFact.GetScheduler();
            // Start up the scheduler (nothing can actually run until the scheduler has been started)
            this._sched.Start();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Scheduler getInstance()
        {
            if (_instance == null)
            {
                _instance = new Scheduler();
            }
            return _instance;
        } //End getInstance

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddJobToSchedule(PhocalstreamJob job)
        {
            // construct job info
            IJobDetail jobDetail = new JobDetailImpl(job.GetType().ToString(), null, job.GetType());

            ICronTrigger trigger = (ICronTrigger)TriggerBuilder.Create()
                .WithIdentity(job.GetType().ToString() + "trigger", null)
                .WithCronSchedule(job.GetSchedule())
                .Build();
             
            // Tell quartz to schedule the job using the trigger
            this._sched.ScheduleJob(jobDetail, trigger);
        } //End AddJobToSchedule
    }

    public class DmImporterJob : PhocalstreamJob
    {
        private string _sched;

        public DmImporterJob()
        {
            //Currently hardcoded to run every Thursday @ 12:00 PM
            this._sched = "0 0 12 ? * THU";
        }

        public string GetSchedule()
        {
            return this._sched;
        }

        public void Execute(IJobExecutionContext context)
        {
            DroughtMonitorImporter.getInstance().RunDMImport(DateTime.Now);
        }
    }

}