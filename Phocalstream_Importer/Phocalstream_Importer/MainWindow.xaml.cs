using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Phocalstream_Web.Models;
using Phocalstream_Web.Application;
using System.Drawing;
using System.Drawing.Imaging;
using Phocalstream_Importer.ViewModels;
using System.Data;
using System.Collections.ObjectModel;
using Microsoft.DeepZoomTools;
using Ionic.Zip;

namespace Phocalstream_Importer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CameraSiteViewModel _viewModel = new CameraSiteViewModel();

        public MainWindow()
        {
            _viewModel.Site = new CameraSite();
            _viewModel.ProgressTotal = 1;
            using (EntityContext ctx = new EntityContext())
            {
                _viewModel.SiteList = new ObservableCollection<CameraSite>(ctx.Sites.Include("Photos").ToList<CameraSite>());
            }
            
            InitializeComponent();
            base.DataContext = _viewModel;
        }

        private void DeleteSelected(object sender, RoutedEventArgs e)
        {
            CameraSite selected = (CameraSite) this.Sites.SelectedItem;
            if (selected != null)
            {
                using (EntityContext ctx = new EntityContext())
                {
                    ctx.Sites.Remove(ctx.Sites.Attach(selected));
                    ctx.Entry<CameraSite>(selected).State = EntityState.Deleted;
                    ctx.SaveChanges();
                }

                CloudStorageAccount account = CloudStorageAccount.Parse(
                    String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", _viewModel.StorageAccountName, _viewModel.StorageAccountKey));

                CloudBlobClient client = account.CreateCloudBlobClient();

                CloudBlobContainer container = client.GetContainerReference(selected.ContainerID);
                container.Delete();

                _viewModel.SiteList.Remove(selected);
            }
        }

        private Object lockObj = new Object();
        int available = 7;

        private void BeginImport(object sender, RoutedEventArgs e)
        {
            ThreadStart t = delegate()
            {
                using (EntityContext ctx = new EntityContext())
                {
                    if (_viewModel.ContainerName == null || _viewModel.ContainerName.Trim() == "")
                    {
                        _viewModel.ContainerName = String.Format("prtlp-{0}", DateTime.Now.Ticks);
                        _viewModel.Site.Photos = new List<Photo>();
                        ctx.Sites.Add(_viewModel.Site);
                    }
                    else
                    {
                        List<CameraSite> sites = (from s in ctx.Sites where s.ContainerID == _viewModel.ContainerName select s).ToList<CameraSite>();
                        if (sites.Count == 1)
                        {
                            _viewModel.Site = sites.ElementAt<CameraSite>(0);
                        }
                        else
                        {
                            _viewModel.Site.Photos = new List<Photo>();
                            ctx.Sites.Add(_viewModel.Site);
                        }
                    }
                    ctx.SaveChanges();
                }

                CloudStorageAccount account = CloudStorageAccount.Parse(
                    String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", _viewModel.StorageAccountName, _viewModel.StorageAccountKey));

                CloudBlobClient client = account.CreateCloudBlobClient();

                CloudBlobContainer container = client.GetContainerReference(_viewModel.ContainerName);
                container.CreateIfNotExist();

                string[] files = Directory.GetFiles(_viewModel.ImagePath, "*.JPG", SearchOption.AllDirectories);
                _viewModel.ProgressTotal = files.Length;
                _viewModel.ProgressValue = 0;

                BlobRequestOptions opts = new BlobRequestOptions() { Timeout = TimeSpan.FromMinutes(20) };
                int len = files.Length;
                int current = 0;

                while ( current < len || (available != 7 || current == 0))
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

                _viewModel.ProgressColor = "Gray";
                _viewModel.Site = new CameraSite();
                using (EntityContext ctx = new EntityContext())
                {
                    _viewModel.SiteList = new ObservableCollection<CameraSite>(ctx.Sites.Include("Photos").ToList<CameraSite>());
                }
                MessageBox.Show("Import process complete");
            };
            new Thread(t).Start();
        }

        private void ProcessFile(string fileName, CloudBlobContainer container, BlobRequestOptions opts)
        {
            lock (lockObj)
            {
                available--;
            }
            _viewModel.CurrentStatus = String.Format("Processing image {0} ...", fileName);
            using (var fileStream = System.IO.File.OpenRead(fileName))
            {
                using (EntityContext ctx = new EntityContext())
                {
                    CameraSite site = (from s in ctx.Sites where s.ID == _viewModel.Site.ID select s).First<CameraSite>();
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
                _viewModel.ProgressValue = _viewModel.ProgressValue + 1;
                available++;
            }
        }

        private void Sites_Selected_1(object sender, RoutedEventArgs e)
        {
        }
    }
}
