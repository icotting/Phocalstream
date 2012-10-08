using Ionic.Zip;
using Microsoft.DeepZoomTools;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Phocalstream_Importer.Commands;
using Phocalstream_Web.Application;
using Phocalstream_Web.Models;
using System;
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
        int available = 7;

        protected void BeginImport()
        {
            ThreadStart t = delegate()
            {
                using (EntityContext ctx = new EntityContext())
                {
                    if (this.ContainerName == null || this.ContainerName.Trim() == "")
                    {
                        this.ContainerName = String.Format("prtlp-{0}", DateTime.Now.Ticks);
                        this.Site.Photos = new List<Photo>();
                        ctx.Sites.Add(this.Site);
                    }
                    else
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

                CloudStorageAccount account = CloudStorageAccount.Parse(
                    String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", this.StorageAccountName, this.StorageAccountKey));

                CloudBlobClient client = account.CreateCloudBlobClient();

                CloudBlobContainer container = client.GetContainerReference(this.ContainerName);
                container.CreateIfNotExist();

                string[] files = Directory.GetFiles(this.ImagePath, "*.JPG", SearchOption.AllDirectories);
                this.ProgressTotal = files.Length;
                this.ProgressValue = 0;

                BlobRequestOptions opts = new BlobRequestOptions() { Timeout = TimeSpan.FromMinutes(20) };
                int len = files.Length;
                int current = 0;

                while (current < len || (available != 7 || current == 0))
                {
                    lock (lockObj)
                    {
                        if (available > 0 && current < len)
                        {
                            new Thread(() => ProcessFile(files[current++], container, opts)).Start();
                        }
                    }
                    Thread.Sleep(500);
                }

                this.ProgressColor = "Gray";
                this.Site = new CameraSite();
                using (EntityContext ctx = new EntityContext())
                {
                    this.SiteList = new ObservableCollection<CameraSite>(ctx.Sites.Include("Photos").ToList<CameraSite>());
                }
            };
            new Thread(t).Start();
        }

        private void ProcessFile(string fileName, CloudBlobContainer container, BlobRequestOptions opts)
        {
            lock (lockObj)
            {
                available--;
            }
            this.CurrentStatus = String.Format("Processing image {0} ...", fileName);
            using (var fileStream = System.IO.File.OpenRead(fileName))
            {
                using (EntityContext ctx = new EntityContext())
                {
                    CameraSite site = (from s in ctx.Sites where s.ID == this.Site.ID select s).First<CameraSite>();
                    System.Drawing.Image img = new Bitmap(fileStream);
                    PropertyItem[] propItems = img.PropertyItems;
                    Photo photo = new Photo();
                    photo.BlobID = Guid.NewGuid().ToString();
                    photo.Site = site;
                    ctx.Photos.Add(photo);

                    photo.AdditionalExifProperties = new List<MetaDatum>();
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
                    site.Photos.Add(photo);
                    ctx.SaveChanges();

                    string rootPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), photo.BlobID);
                    Directory.CreateDirectory(rootPath);
                    File.Copy(fileName, System.IO.Path.Combine(rootPath, "raw.JPG"));

                    ImageCreator creator = new ImageCreator();
                    creator.TileFormat = Microsoft.DeepZoomTools.ImageFormat.Jpg;
                    creator.TileOverlap = 1;
                    creator.TileSize = 256;
                    creator.Create(fileName, System.IO.Path.Combine(rootPath, "source.dzi"));
                    
                    using (MemoryStream pstream = new MemoryStream())
                    {
                        using (ZipFile zip = new ZipFile())
                        {
                            zip.AddDirectory(rootPath);
                            zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                            zip.Save(pstream);
                        }

                        pstream.Position = 0;

                        CloudBlob blob = container.GetBlobReference(photo.BlobID);
                        blob.UploadFromStream(pstream, opts);
                    }

                    Directory.Delete(rootPath, true);
                    img.Dispose();
                }
            }

            lock (lockObj)
            {
                this.ProgressValue = this.ProgressValue + 1;
                available++;
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
