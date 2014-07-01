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
		private string Directory { get { return ConfigurationManager.AppSettings["outputPath"] + "/Job" + Id + "/"; } }
		private string TemporaryDirectory { get { return ConfigurationManager.AppSettings["outputPath"] + "/Job" + Id + "/temp/"; } }

		private readonly bool Verbose = true;
		private readonly bool ProduceLog = true;

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
			Log("Beginning processing for " + Id);
			Composer = Task.Run(() => ProcessPhotos());
		}

		private void Log(string message, string title = "")
		{
			if (!ProduceLog) return;
			using(StreamWriter logfile = File.AppendText(ConfigurationManager.AppSettings["outputPath"] + "/phlog.txt"))
			{
				logfile.Write("[");
				logfile.Write(DateTime.Now.ToShortTimeString());
				logfile.Write("]: ");
				logfile.WriteLine(title);
				logfile.WriteLine(message);
				logfile.WriteLine();
			}
		}

		private void ProcessPhotos()
		{
			List<string> photoFilenames = PhotoFilenames();
			new FileInfo(TemporaryDirectory).Directory.Create();

			Log("Creating blend frames for " + Id);
			// Skip one file so that all files can be blended with the previous.
			for (int i = 1; i < photoFilenames.Count; ++i)
			{
				CreateBlend(photoFilenames[i - 1], photoFilenames[i], "blended" + i.ToString("D09") + ".jpg");
			}

			// Wait until all the blends have been finished.
			int finalFileCount = (photoFilenames.Count-1) * 3;
			while(new FileInfo(TemporaryDirectory).Directory.GetFiles().Length < finalFileCount)
			{
				Log("Waiting for blend files." + new FileInfo(TemporaryDirectory).Directory.GetFiles().Length + ",,," + finalFileCount);
				Thread.Sleep(1000);
			}

			Log("Creating mpeg for " + Id);
			CreateMpeg(TemporaryDirectory + "blended%09d.jpg", Framerate, Destination);
			if(completionEvent != null)
			{
				completionEvent(this);
			}
		}

		private void CreateBlend(string imageA, string imageB, string destination)
		{
			if(Verbose)
			{
				Log("Creating blend for [" + imageA + "] and [" + imageB + "] -> " + destination);
			}
			using (StreamWriter stream = new StreamWriter(File.OpenWrite(TemporaryDirectory + imageA.Split('\\', '/').Last() + imageB.Split('\\', '/').Last() + "condor.submit")))
			{
				stream.WriteLine("Universe = vanilla");
				stream.WriteLine("Executable = " + TemporaryDirectory + imageA.Split('\\', '/').Last() + imageB.Split('\\', '/').Last() + "exec.bat");
				stream.WriteLine("getenv = true");
				stream.WriteLine("run_as_owner = true");
				stream.WriteLine("Queue");
			}
			using (StreamWriter stream = new StreamWriter(File.OpenWrite(TemporaryDirectory + imageA.Split('\\', '/').Last() + imageB.Split('\\', '/').Last() + "exec.bat")))
			{
				stream.Write(ConfigurationManager.AppSettings["magickPath"] + "/composite.exe -blend 20 \"" + ExtractImagePath(imageA) + "\" -matte \"" + ExtractImagePath(imageB) + "\" \"" + TemporaryDirectory + destination + "\"");
			}

			Log("Submitting for job " + Id);
			Process process = new Process();
			process.StartInfo.FileName = "condor_submit";
			process.StartInfo.Arguments = "\"" + TemporaryDirectory + imageA.Split('\\', '/').Last() + imageB.Split('\\', '/').Last() + "condor.submit" + "\"";
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.UseShellExecute = false;
			process.Start();
			Log(process.StandardOutput.ReadToEnd().Trim(), "Blend output");
			Log(process.StandardError.ReadToEnd().Trim(), "Blend error");
		}

		private void CreateMpeg(string path, int framerate, string destination)
		{
			string ffmpeg = ConfigurationManager.AppSettings["ffmpegPath"] + "\\ffmpeg";
			string arguments = "-f image2 -r " + framerate + " -i " + path + " -vf scale=2000:-1 -qscale 2 -r 20 \"" + destination + "\"";

			using (StreamWriter stream = new StreamWriter(File.OpenWrite(Directory + "condor.submit")))
			{
				stream.WriteLine("Universe = vanilla");
				stream.WriteLine("Executable = " + ffmpeg);
				stream.WriteLine("Arguments = " + arguments);
				stream.WriteLine("Queue");
			}

			Log(ffmpeg + " " + arguments);
			Process process = new Process();
			process.StartInfo.FileName = "condor_submit";
			process.StartInfo.Arguments = Directory + "condor.submit";
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.UseShellExecute = false;
			process.Start();
			while (!process.StandardError.EndOfStream)
			{
				Log(process.StandardError.ReadLine(), "Condor error");
			}
			while(!process.StandardOutput.EndOfStream)
			{
				Log(process.StandardOutput.ReadLine(), "Condor output");
			}
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
