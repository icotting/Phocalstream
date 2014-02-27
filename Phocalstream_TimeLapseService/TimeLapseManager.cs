using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_TimeLapseService
{
	public class TimeLapseManager : MarshalByRefObject, ITimeLapseManager
	{
		// To keep track of running jobs, for shutdown and status checking.
		private Dictionary<long, TimeLapseJob> Jobs { get; set; }

		private long MinimumId { get; set; }
		private long NextId { get; set; }
		private Stack<long> UnusedIds { get; set; }

		private const long MaximumSittingJobs = 500;

		public TimeLapseManager()
		{
			Jobs = new Dictionary<long, TimeLapseJob>();
			UnusedIds = new Stack<long>();
			NextId = 0;
			MinimumId = 0;
		}

		public void ClearJobs()
		{
			Jobs.Clear();
		}

		private long ExtractOldestId()
		{
			Stack<long> temporary = new Stack<long>();
			while (UnusedIds.Count > 0)
			{
				temporary.Push(UnusedIds.Pop());
			}
			long bottom = temporary.Pop();
			while (temporary.Count > 0)
			{
				UnusedIds.Push(temporary.Pop());
			}
			return bottom;
		}

		public long StartJob(List<long> photoIds, int framerate)
		{
			TimeLapseJob job;
			if(UnusedIds.Count == 0)
			{
				job = new TimeLapseJob(NextId, photoIds, framerate);
				NextId += 1;
				if(NextId > MaximumSittingJobs)
				{
					Directory.Delete(ConfigurationManager.AppSettings["outputPath"] + "/Job" + (NextId - MaximumJobs), true);
					MinimumId += 1;
				}
			}
			else
			{
				long id = UnusedIds.Pop();
				if(id < MinimumId)
				{
					job = new TimeLapseJob(NextId, photoIds, framerate);
					NextId += 1;
					if (NextId > MaximumSittingJobs)
					{
						Directory.Delete(ConfigurationManager.AppSettings["outputPath"] + "/Job" + (NextId - MaximumJobs), true);
						MinimumId += 1;
					}
				}
				else
				{
					job = new TimeLapseJob(id, photoIds, framerate);
				}
			}

			job.BeginProcessing();
			Jobs.Add(job.Id, job);
			return job.Id;
		}

		public float CheckJob(long jobId)
		{
			TimeLapseJob job;
			Jobs.TryGetValue(jobId, out job);
			if(job == null)
			{
				return 0f;
			}
			return job.Progress;
		}

		public void ExportJobs(string filename)
		{
			string[] lines = new string[UnusedIds.Count+1];
			lines[0] = NextId.ToString();
			for(int i = 1; i < lines.Length; ++i)
			{
				lines[i] = UnusedIds.Pop().ToString();
			}
			File.WriteAllLines(filename, lines);
		}

		public void ImportJobs(string filename)
		{
			if(!File.Exists(filename))
			{
				return;
			}
			string[] lines = File.ReadAllLines(filename);
			NextId = long.Parse(lines[0]);
			for (int i = 1; i < lines.Length; ++i)
			{
				UnusedIds.Push(long.Parse(lines[i]));
			}
		}

		public string GetJobDestination(long jobId)
		{
			TimeLapseJob job;
			Jobs.TryGetValue(jobId, out job);
			if(job == null)
			{
				return "No known job with that id";
			}
			return job.Destination;
		}

		public void DiscardJob(long jobId)
		{
			TimeLapseJob job;
			Jobs.TryGetValue(jobId, out job);
			if(job == null)
			{
				return;
			}

			// We only want to add the id to the unused Id stack
			// when the job is actually done, so that there
			// isn't any conflict over the file system.
			if (job.Progress <= 0.99f)
			{
				job.completionEvent += job_discardedCompletionEvent;
			}
			else
			{
				HardDiscard(jobId);
			}

			Jobs.Remove(jobId);
		}

		private void job_discardedCompletionEvent(TimeLapseJob sender)
		{
			HardDiscard(sender.Id);
		}

		private void HardDiscard(long jobId)
		{
			if(jobId > MinimumId)
			{
				UnusedIds.Push(jobId);
			}
		}
	}
}
