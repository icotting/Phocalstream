using Microsoft.DeepZoomTools;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Phocalstream_Importer.Commands;
using Phocalstream_Web.Application;
using Phocalstream_Shared.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Phocalstream_Shared;

namespace Phocalstream_Importer.ViewModels
{
    class CameraSiteViewModel : BindableObject
    {

        private int _progressTotal;
        private int _progressValue;
        private string _progressColor;

        private CameraSite _site;

        public CameraSite Site
        {
            get { return _site; }
            set { _site = value; }
        }

        public string StorageAccountName { get; set; }
        public string StorageAccountKey { get; set; }
        public string ImagePath { get; set; }

        private string _currentStatus;
        public string CurrentStatus 
        {
            get { return _currentStatus; }
            set { _currentStatus = value; this.RaisePropertyChanged("CurrentStatus"); }        
        }

        public int ProgressValue
        {
            get { return _progressValue; }
            set { _progressValue = value; this.RaisePropertyChanged("ProgressValue"); }
        }

        public int ProgressTotal
        {
            get { return _progressTotal; }
            set { _progressTotal = value; this.RaisePropertyChanged("ProgressTotal"); }
        }

        public string ProgressColor
        {
            get { return _progressColor; }
            set { _progressColor = value; this.RaisePropertyChanged("ProgressColor"); }
        }

        public string SiteName
        {
            get { return _site.Name; }
            set { _site.Name = value; this.RaisePropertyChanged("SiteName"); }
        }

        public double Latitude
        {
            get { return _site.Latitude; }
            set { _site.Latitude = value; this.RaisePropertyChanged("Latitude"); }
        }

        public double Longitude
        {
            get { return _site.Longitude; }
            set { _site.Longitude = value; this.RaisePropertyChanged("Longitude"); }
        }

        public string ContainerName
        {
            get { return _site.ContainerID; }
            set { _site.ContainerID = value; this.RaisePropertyChanged("ContainerName"); }
        }

        public int SelectedSiteIndex { get; set; }

        public ICommand ImportPhotos
        {
            get { return new RelayCommand(BeginImport); }
        }

        public ICommand DeleteSite
        {
            get { return new RelayCommand(DeleteSelected); }
        }

        public ICommand RunDeepZoomProcess 
        {
            get { return new RelayCommand(doProcess); }
        }

        public ObservableCollection<CameraSite> SiteList { get; set; }

        private Object lockObj = new Object();

        protected void BeginImport()
        {
            new Task(() => DoImport()).Start();
        }

        protected void DoImport()
        {
            using (EntityContext ctx = new EntityContext())
            {
                // create a new container if one has not been provided
                if (this.ContainerName == null || this.ContainerName.Trim() == "")
                {
                    this.ContainerName = String.Format("prtlp-{0}", DateTime.Now.Ticks);
                    this.Site.Photos = new List<Photo>();
                    ctx.Sites.Add(this.Site);
                }
                else // if the container was provided, load the contents
                {
                    List<CameraSite> sites = (from s in ctx.Sites where s.ContainerID == this.ContainerName select s).ToList<CameraSite>();
                    if (sites.Count == 1)
                    {
                        this.Site = sites.ElementAt<CameraSite>(0);
                    }
                    else
                    {
                        this.Site.Photos = new List<Photo>();
                        ctx.Sites.Add(this.Site);
                    }
                }
                ctx.SaveChanges();
            }

            // get a connection to the blob storage account
            CloudStorageAccount account = CloudStorageAccount.Parse(
                String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", this.StorageAccountName, this.StorageAccountKey));

            CloudBlobClient client = account.CreateCloudBlobClient();

            // create a reference to the container for this image site
            CloudBlobContainer container = client.GetContainerReference(this.ContainerName);
            BlobRequestOptions options = new BlobRequestOptions();
            options.UseFlatBlobListing = true;
            container.CreateIfNotExist(); // create it on the cloud if it isn't already there

            // set permissions to be public so anyone can access the image sets
            BlobContainerPermissions blobContainerPermissions = new BlobContainerPermissions();
            blobContainerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            container.SetPermissions(blobContainerPermissions);

            // grab all of the raw images from the provided root directory
            string[] files = Directory.GetFiles(this.ImagePath, "*.JPG", SearchOption.AllDirectories);
            this.ProgressTotal = files.Length;
            this.ProgressValue = 0;

            // set the timeout for upload to 30min to allow enough time for high latencey and/or big files
            BlobRequestOptions opts = new BlobRequestOptions() { Timeout = TimeSpan.FromMinutes(20) };

            // setup the producer consumer queue to control the number of running threads
            ConcurrentQueue<Task> waitLine = new ConcurrentQueue<Task>();
            BlockingCollection<Task> queue = new BlockingCollection<Task>(waitLine, 25);
            Task.Factory.StartNew(() =>
            {
                foreach (string file in files)
                {
                    // for each raw image, create a processing task
                    Task task = new Task(() => ProcessFile(file, container, opts));
                    queue.Add(task); // add the task to the queue - this will block the thread if the queue is full
                    task.Start(); // when the task is done, it will be removed from the queue
                }
                queue.CompleteAdding(); // notify the consumer that all tasks have been processed
            });

            Task.Factory.StartNew(() =>
            {
                while (queue.IsCompleted == false)
                {
                    Task check;
                    waitLine.TryPeek(out check); // peek at the task at the head of the queue
                    if (check != null && check.IsCompleted)
                    {
                        queue.Take(); // if the task is finished, remove it from the waiting queue
                    }
                }

                this.ProgressColor = "Gray";
                this.Site = new CameraSite();
                using (EntityContext ctx = new EntityContext())
                {
                    // update the ViewModel site list
                    this.SiteList = new ObservableCollection<CameraSite>(ctx.Sites.Include("Photos").ToList<CameraSite>());
                }
                queue.Dispose();
            });
        }

        private void ProcessFile(string fileName, CloudBlobContainer container, BlobRequestOptions opts)
        {
            // open a stream to the raw image
            using (var fileStream = System.IO.File.OpenRead(fileName))
            {
                // get an entity context for adding the photo entity for this image
                using (EntityContext ctx = new EntityContext())
                {
                    // find the camera site for this photo (bound to this entity context)
                    CameraSite site = (from s in ctx.Sites where s.ID == this.Site.ID select s).First<CameraSite>();

                    // open a Bitmap for the image to parse the meta data from
                    using (System.Drawing.Image img = new Bitmap(fileStream))
                    {
                        // get image mea data
                        PropertyItem[] propItems = img.PropertyItems;

                        // create a new photo with a GUID id for the cloud blob
                        Photo photo = new Photo();
                        photo.BlobID = Guid.NewGuid().ToString();
                        photo.Site = site;
                        photo.AdditionalExifProperties = new List<MetaDatum>();
                        ctx.Photos.Add(photo);

                        // walk the image properties and set the appropriate fields on the image for the various meta data types (EXIF)
                        int len = propItems.Length;
                        for (var i = 0; i < len; i++)
                        {
                            PropertyItem propItem = propItems[i];

                            switch (propItem.Id)
                            {
                                case 0x829A: // Exposure Time
                                    photo.ExposureTime = Convert.ToDouble(BitConverter.ToInt32(propItem.Value, 0)) / Convert.ToDouble(BitConverter.ToInt32(propItem.Value, 4));
                                    photo.ShutterSpeed = String.Format("{0}/{1}", BitConverter.ToUInt32(propItem.Value, 0), BitConverter.ToUInt32(propItem.Value, 4));
                                    break;
                                case 0x0132: // Date
                                    string[] parts = System.Text.Encoding.ASCII.GetString(propItem.Value).Split(':', ' ');
                                    int year = int.Parse(parts[0]);
                                    int month = int.Parse(parts[1]);
                                    int day = int.Parse(parts[2]);
                                    int hour = int.Parse(parts[3]);
                                    int minute = int.Parse(parts[4]);
                                    int second = int.Parse(parts[5]);

                                    photo.Captured = new DateTime(year, month, day, hour, minute, second);
                                    break;
                                case 0x010F: // Manufacturer
                                    photo.AdditionalExifProperties.Add(new MetaDatum()
                                    {
                                        Photo = photo,
                                        Name = "Manufacturer",
                                        Type = "EXIF",
                                        Value = System.Text.Encoding.ASCII.GetString(propItem.Value)
                                    });
                                    break;
                                case 0x5090: // Luminance
                                    photo.AdditionalExifProperties.Add(new MetaDatum()
                                    {
                                        Photo = photo,
                                        Name = "White Balance",
                                        Type = "EXIF",
                                        Value = Convert.ToString(BitConverter.ToUInt16(propItem.Value, 0))
                                    });
                                    break;
                                case 0x5091: // Chrominance
                                    photo.AdditionalExifProperties.Add(new MetaDatum()
                                    {
                                        Photo = photo,
                                        Name = "Color Space",
                                        Type = "EXIF",
                                        Value = Convert.ToString(BitConverter.ToUInt16(propItem.Value, 0))
                                    });
                                    break;
                                case 0x9205: // Max Aperture
                                    photo.MaxAperture = Convert.ToDouble(BitConverter.ToInt32(propItem.Value, 0)) / Convert.ToDouble(BitConverter.ToInt32(propItem.Value, 4));
                                    break;
                                case 0x920A: // Focal Length
                                    photo.FocalLength = BitConverter.ToInt32(propItem.Value, 0) / BitConverter.ToInt32(propItem.Value, 4);
                                    break;
                                case 0x9209: // Flash
                                    photo.Flash = Convert.ToBoolean(BitConverter.ToUInt16(propItem.Value, 0));
                                    break;
                                case 0x9286: // Comment
                                    photo.UserComments = System.Text.Encoding.ASCII.GetString(propItem.Value);
                                    break;
                                case 0x8827: // ISO Speed
                                    photo.ISO = BitConverter.ToUInt16(propItem.Value, 0);
                                    break;
                            }
                        }
                        ctx.SaveChanges(); // persist the photo to the db

                        // reset the file stream so it can be uploaded to the cloud
                        fileStream.Position = 0;
                        CloudBlob blob = container.GetBlobReference(String.Format("{0}/Image.jpg", photo.BlobID));
                        blob.UploadFromStream(fileStream, opts); // upload the raw image
                    }
                }
            }
            File.Delete(fileName);

            lock (lockObj)
            {
                this.ProgressValue = this.ProgressValue + 1;
            }
        }

        protected void DeleteSelected()
        {
            CameraSite selected = this.SiteList.ElementAt<CameraSite>(this.SelectedSiteIndex);
            if (selected != null)
            {
                using (EntityContext ctx = new EntityContext())
                {
                    ctx.Sites.Remove(ctx.Sites.Attach(selected));
                    ctx.Entry<CameraSite>(selected).State = EntityState.Deleted;
                    ctx.SaveChanges();
                }

                CloudStorageAccount account = CloudStorageAccount.Parse(
                    String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", this.StorageAccountName, this.StorageAccountKey));

                CloudBlobClient client = account.CreateCloudBlobClient();

                CloudBlobContainer container = client.GetContainerReference(selected.ContainerID);
                container.Delete();

                this.SiteList.Remove(selected);
            }
        }
        private void doProcess() 
        {
            new Task(() => {
                try
                {
                    // get a connection to the blob storage account
                    CloudStorageAccount account = CloudStorageAccount.Parse(
                        String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", this.StorageAccountName, this.StorageAccountKey));
                    CloudBlobClient client = account.CreateCloudBlobClient();

                    // create a DeepZoom image creater to generate the tile set for each raw image
                    ImageCreator creator = new ImageCreator();
                    creator.TileFormat = Microsoft.DeepZoomTools.ImageFormat.Jpg;
                    creator.TileOverlap = 1;
                    creator.TileSize = 256;

                    CollectionCreator ccreator = new CollectionCreator();
                    ccreator.TileFormat = Microsoft.DeepZoomTools.ImageFormat.Jpg;
                    ccreator.TileOverlap = 1;
                    ccreator.TileSize = 256;

                    this.CurrentStatus += "Generating DeepZoom Tiles ...\n";
                    string rootPath = "";
                    string containerID = null;
                    string siteName = null;
                    long siteId = -1;

                    List<string> files = null;
                    List<Tuple<string, string, long, string>> siteInfo = new List<Tuple<string, string, long, string>>();

                    using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand command = new SqlCommand("select BlobID,ContainerID,Site_ID,s.Name from Photos p inner join CameraSites s on p.Site_ID = s.ID where s.ID not in (select Site_ID from Collections where Status = 1) order by Site_ID", conn))
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                siteInfo.Add(new Tuple<string, string, long, string>(reader.GetString(0), reader.GetString(1), reader.GetInt64(2), reader.GetString(3)));
                            }
                        }
                        conn.Close();
                    }

                    foreach (Tuple<string, string, long, string> site in siteInfo)
                    {
                        if (containerID == null || containerID != site.Item2)
                        {
                            if (files != null)
                            {
                                CompleteContainer(files, client, containerID, siteName, siteId, rootPath, ccreator);
                            }

                            // create a directory in which to store the DeepZoom tiles for the image
                            rootPath = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.GetTempPath(), @"dzgen"), site.Item2);
                            Directory.CreateDirectory(rootPath);

                            // reset files to begin processing a new site
                            files = new List<string>();
                            containerID = site.Item2;
                            siteId = site.Item3;
                            siteName = site.Item4;

                            this.CurrentStatus += String.Format("Starting process in {0}\n", rootPath);
                        }

                        string fileName = System.IO.Path.Combine(rootPath, site.Item1 + ".jpg");
                        string dziFile = System.IO.Path.Combine(rootPath, site.Item1 + ".dzi");

                        if (File.Exists(dziFile) == false) // if the file is already there, don't recreate it
                        {
                            CloudBlob imageBlob = client.GetBlobReference(site.Item2 + "/" + site.Item1 + "/Image.jpg");
                            try
                            {
                                imageBlob.DownloadToFile(fileName);
                                creator.Create(fileName, dziFile); // create the DeepZoom tileset
                                File.Delete(fileName);
                                files.Add(dziFile);
                            }
                            catch (Exception e)
                            {
                                this.CurrentStatus += String.Format("Could not process file {0} due to {1}\n", fileName, e.Message);
                            }
                        }
                    }
                    CompleteContainer(files, client, containerID, siteName, siteId, rootPath, ccreator);
                }
                catch (Exception e)
                {
                    this.CurrentStatus += String.Format("Error: {0}\n", e.Message);
                    this.CurrentStatus += String.Format("Error: {0}\n", e.ToString());
                }

                this.CurrentStatus += "Deep Zoom Process complete\n";            
            }).Start();
        }

        private void CompleteContainer(List<string> files, CloudBlobClient client, string containerID, string siteName, long siteId, string rootPath, CollectionCreator ccreator)
        {
                // create a reference to the container for this image site
                CloudBlobContainer container = client.GetContainerReference(String.Format("{0}-dz", containerID));
                container.CreateIfNotExist(); // create it on the cloud if it isn't already there

                BlobRequestOptions options = new BlobRequestOptions();
                options.UseFlatBlobListing = true;

                // set permissions to be public so anyone can access the image sets
                BlobContainerPermissions blobContainerPermissions = new BlobContainerPermissions();
                blobContainerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
                container.SetPermissions(blobContainerPermissions);

                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString))
                {
                    conn.Open();
                    Boolean create = false;
                    using (SqlCommand command = new SqlCommand("select ID from Collections where Site_ID = @Site", conn))
                    {
                        command.Parameters.AddWithValue("@Site", siteId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows == false) // there is no collection for this site
                            {
                                create = true;
                            }
                        }
                    }

                    if (create)
                    {
                        this.CurrentStatus += String.Format("Building DeepZoom Collection for {0}...", containerID);
                        // generate the collection
                        ccreator.Create(files, System.IO.Path.Combine(rootPath, "da.dzc"));

                        using (SqlCommand insert = new SqlCommand("insert into Collections (Name,Type,Site_ID,Status) values (@Name,@Type,@Site_ID,@Status)", conn))
                        {
                            insert.Parameters.AddWithValue("@Name", siteName);
                            insert.Parameters.AddWithValue("@Type", 0);
                            insert.Parameters.AddWithValue("@Site_ID", siteId);
                            insert.Parameters.AddWithValue("@Status", 0);
                            insert.ExecuteNonQuery();
                        }
                    }
                    conn.Close();
                }
                DirectoryInfo rootInfo = new DirectoryInfo(rootPath);

                this.CurrentStatus += "Uploading data to Azure ...\n";
                IterateFolders(rootInfo, container, rootPath, rootPath); // upload all of the generated image tiles
                this.CurrentStatus += String.Format("Cleaning up for {0} ...\n", containerID);

                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand command = new SqlCommand("update Collections set Status = 1 where Site_ID = @Site", conn))
                    {
                        command.Parameters.AddWithValue("@Site", siteId);
                        command.ExecuteNonQuery();
                    }
                    conn.Close();
                }
                Directory.Delete(rootPath, true); // delete the local DeepZoom tiles
        }

        // this method was taken from http://blogs.msdn.com/b/jbarnes/archive/2011/12/07/hosting-wp7-deep-zoom-content-in-azure-blob-storage.aspx 
        private static void IterateFolders(DirectoryInfo CurrentDir, CloudBlobContainer TargetContainer, string RootFolderName, string globalRoot)
        {
            DirectoryInfo[] ChildDirectories = CurrentDir.GetDirectories();

            //recurively iterate through all descendants of the source folder
            foreach (DirectoryInfo ChildDir in ChildDirectories)
            {
                IterateFolders(ChildDir, TargetContainer, RootFolderName, globalRoot);
            }

            //get the path name including only the rootfoldername and its decendants; it will be used as part of the filename
            string PreAppendPath = CurrentDir.FullName.Remove(0, CurrentDir.FullName.IndexOf(RootFolderName));

            //get file list
            FileInfo[] FileList = CurrentDir.GetFiles();

            //Iterate through all files in a Folder in PARALLEL
            Parallel.ForEach(FileList, file =>
            {
                //filename + path and use as name in container; path + filename should be unique
                string NewFileName = PreAppendPath + "\\" + file.Name;

                //Change Slash to opposite directon
                string FldrPath = globalRoot;
                FldrPath = FldrPath.Replace(@"\", "/").ToLower();

                //Strip relative leading path
                NewFileName = NewFileName.Replace(@"\", "/").ToLower();
                NewFileName = NewFileName.Replace(FldrPath, "");

                //Strip leading slash for root documents
                if (NewFileName.IndexOf("/") == 0)
                    NewFileName = NewFileName.Remove(0, 1);

                try
                {
                    //Upload Blob
                    BlobRequestOptions options = new BlobRequestOptions() { Timeout = TimeSpan.FromMinutes(20) };
                    CloudBlob destBlob = TargetContainer.GetBlobReference(NewFileName);
                    destBlob.UploadFile(file.FullName, options);
                    file.Delete();
                }
                catch (Exception e)
                {
                    Console.WriteLine(String.Format("Error uploading file {0}", NewFileName);
                    Console.WriteLine(e.ToString());
                }
            });
        }
    }
}
