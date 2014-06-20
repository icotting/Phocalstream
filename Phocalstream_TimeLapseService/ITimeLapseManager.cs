using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_TimeLapseService
{
	/// <summary>
	/// Recommended use of a TimeLapseManager:
	/// 
	/// StartJob(the photos to process, the mpeg framerate)
	/// if the user checks status:
	///     CheckJob(jobId)
	///     if it's done:
	///         get the destination with GetJobDestination
	///         DiscardJob(jobId)
	///     else
	///         tell the user progress
	/// </summary>
	public interface ITimeLapseManager
	{
		/// <summary>
		/// Starts a TimeLapseJob and gives an ID back for
		/// tracking.
		/// </summary>
		/// <returns>
		/// The id of the TimeLapseJob for future queries.
		/// </returns>
		long StartJob(List<long> photoIds, int framerate);

		/// <summary>
		/// Check how far a job is towards completion (0.0 to 
		/// 1.0 +- epsilon).
		/// </summary>
		float CheckJob(long jobId);

		/// <summary>
		/// Gets the filename where the completed mpg will be 
		/// placed for  the given job id. If the job id cannot 
		/// be identified, will give a empty string.
		/// </summary>
		string GetJobDestination(long jobId);

		/// <summary>
		// Discards the completed or not completed job.
		/// </summary>
		void DiscardJob(long jobId);

		/// <summary>
		// Clear all jobs.
		/// </summary>
		void ClearJobs();

		/// <summary>
		/// Exports all undiscarded jobs to a file
		/// </summary>
		void ExportJobs(string filename);

		/// <summary>
		/// Imports undiscarded jobs from a file
		/// </summary>
		void ImportJobs(string filename);
	}
}
