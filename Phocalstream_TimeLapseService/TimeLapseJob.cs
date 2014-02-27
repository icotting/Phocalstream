using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Phocalstream_TimeLapseService
{
	public class TimeLapseJob
	{
		public long Id { get; private set; }
		public int Framerate { get; private set; }
		public List<long> PhotoIds { get; set; }

		private Mutex progressMutex = new Mutex();
		private float progress = 0f;

		private Task Composer { get; set; }

		public delegate void CompletionHandler(TimeLapseJob sender);
		public event CompletionHandler completionEvent;

		public TimeLapseJob(long id, List<long> photoIds, int framerate)
		{
			Id = id;
			PhotoIds = photoIds;
			Framerate = framerate;
		}

		public bool Complete { get { return progress >= 0.99f; } }
		public string Destination { get { return ConfigurationManager.AppSettings["outputPath"] + "/Job" + Id + "/output.mpg"; } }
		private string TemporaryDirectory { get { return ConfigurationManager.AppSettings["outputPath"] + "/Job" + Id + "/temp/"; } }

		public float Progress
		{
			get
			{
				progressMutex.WaitOne();
				float p = progress;
				progressMutex.ReleaseMutex();
				return p;
			}
			set
			{
				progressMutex.WaitOne();
				progress = value;
				progressMutex.ReleaseMutex();
			}
		}

		public void BeginProcessing()
		{
			Composer = Task.Run(() => Process());
		}

		private void Process()
		{
			List<string> photoFilenames = PhotoFilenames();

			new FileInfo(TemporaryDirectory).Directory.Create();

			LinkedList<Task> blendingTasks = new LinkedList<Task>();
			// Skip one file so that all files can be blended with the previous.
			for (int i = 1; i < photoFilenames.Count; ++i)
			{
				// If I let the closure take i, it will change. This makes another variable which will be redeclared at the beginning of the loop.
				int otherI = i;
				blendingTasks.AddFirst(Task.Run(() => CreateBlend(photoFilenames[otherI - 1], photoFilenames[otherI], TemporaryDirectory + "blended" + otherI.ToString("D09") + ".jpg")));
			}

			// Joins the threads linearly, but this doesn't matter much as there
			// is little disparity between taks run times.
			foreach (var task in blendingTasks)
			{
				task.Wait();
			}

			CreateMpeg(TemporaryDirectory + "blended%09d.jpg", Framerate, Destination);
			if(completionEvent != null)
			{
				completionEvent(this);
			}
		}

		private void CreateMpeg(string path, int framerate, string destination)
		{
			Process ffmpeg = new Process();
			ffmpeg.StartInfo.FileName = ConfigurationManager.AppSettings["ffmpegPath"] + "\\ffmpeg";
			ffmpeg.StartInfo.Arguments = "-f image2 -r " + framerate + " -i " + path + " -vf scale=2000:-1 -qscale 2 -r 20 \"" + destination +"\"";
			ffmpeg.StartInfo.UseShellExecute = false;
			ffmpeg.StartInfo.RedirectStandardInput = true;
			ffmpeg.Start();
			ffmpeg.StandardInput.WriteLine("y");
			ffmpeg.WaitForExit();
			// Approximates that encoding take 30% of the time to complete.
			Progress += 0.3f;
		}

		private void CreateBlend(string imageA, string imageB, string destination)
		{
			Process magick = new Process();
			magick.StartInfo.FileName = ConfigurationManager.AppSettings["magickPath"] + "\\composite";
			magick.StartInfo.Arguments = "-blend 20 \"" + ExtractImagePath(imageA) + "\" -matte \"" + ExtractImagePath(imageB) + "\" " + destination;
			magick.StartInfo.UseShellExecute = false;
			magick.Start();
			magick.WaitForExit();
			// Approximates that blending takes 60% of the time to complete.
			Progress += (1f / (float)(PhotoIds.Count - 1)) * 0.7f;
		}
		
		private string ExtractImagePath(string path)
		{
			//Remove the first three directory things.
			path = path.Substring(path.IndexOf('\\') + 1);
			path = path.Substring(path.IndexOf('\\') + 1);
			path = path.Substring(path.IndexOf('\\') + 1);
			path = ConfigurationManager.AppSettings["rawPath"] + path;
			return path;
		}

		private List<string> PhotoFilenames()
		{
			List<string> photoFiles = new List<string>();

			using (SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString))
			{
				conn.Open();
				foreach (var photoId in PhotoIds)
				{
					// Must be optimized
					using (SqlCommand command = new SqlCommand("select FileName from Photos where ID = @id", conn))
					{
						command.Parameters.AddWithValue("@id", photoId);
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								photoFiles.Add(reader.GetString(0));
							}
						}
					}
				}
			}

			return photoFiles;
		}
	}
}
