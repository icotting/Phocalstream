using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Phocalstream_Importer.Commands;
using Phocalstream_Web.Application;
using Phocalstream_Web.Models.Entity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

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

        public string CurrentStatus { get; set; }

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

        public ObservableCollection<CameraSite> SiteList { get; set; }

        private Object lockObj = new Object();

        protected void BeginImport()
        {
            new Task(() => DoImport()).Start();
        }

        private void DoImport()
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
                foreach ( string file in files)
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
                    if ( check != null && check.IsCompleted )
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
    }
}
