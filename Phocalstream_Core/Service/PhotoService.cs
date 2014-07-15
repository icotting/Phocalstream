using Microsoft.DeepZoomTools;
using Microsoft.Practices.Unity;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Service;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Phocalstream_Service.Service
{
    public class PhotoService : IPhotoService
    {

        [Dependency]
        public IEntityRepository<Photo> PhotoRepository { get; set; }

        [Dependency]
        public IEntityRepository<CameraSite> SiteRepository { get; set; }

        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IPhotoRepository PhotoRepo { get; set; }

        public Collection GetCollectionForProcessing(XmlNode siteData)
        {
            string siteName = siteData["Folder"].InnerText;
            CameraSite site = SiteRepository.Find(s => s.Name == siteName).FirstOrDefault();
            Collection collection = null;

            if (site == null)
            {
                site = new CameraSite(); 
                site.Name = siteData["Folder"].InnerText;
                site.DirectoryName = siteData["Folder"].InnerText;
                site.ContainerID = Guid.NewGuid().ToString();
                if (siteData["Location"].Attributes["latitude"].Value != String.Empty)
                {
                    site.Latitude = Convert.ToDouble(siteData["Location"].Attributes["latitude"].Value);
                    site.Longitude = Convert.ToDouble(siteData["Location"].Attributes["longitude"].Value);
                }

                collection = new Collection()
                {
                    ContainerID = site.ContainerID,
                    Name = site.Name,
                    Site = site,
                    Status = CollectionStatus.PROCESSING,
                    Type = CollectionType.SITE
                };

                CollectionRepository.Insert(collection);
                SiteRepository.Insert(site);
            }
            else
            {
                collection = CollectionRepository.Find(c => c.Site.ID == site.ID).FirstOrDefault();
                collection.Status = CollectionStatus.PROCESSING;
                CollectionRepository.Update(collection);
            }
            
            return collection;
        }

        public Phocalstream_Shared.Data.Model.Photo.Photo ProcessPhoto(string fileName, CameraSite site)
        {
            string relativeName = fileName;
            fileName = Path.Combine(ConfigurationManager.AppSettings["rawPath"], fileName);
            FileInfo info = new FileInfo(fileName);

            try
            {
                // create the directory for the image and its components
                string basePath = Path.Combine(Path.Combine(ConfigurationManager.AppSettings["PhotoPath"], site.DirectoryName), string.Format("{0}.phocalstream", info.Name));
                if (Directory.Exists(basePath) == false)
                {
                    Directory.CreateDirectory(basePath);
                }

                // open a Bitmap for the image to parse the meta data from
                using (System.Drawing.Image img = System.Drawing.Image.FromFile(fileName))
                {
                    // get image mea data
                    PropertyItem[] propItems = img.PropertyItems;

                    // create a new photo with a GUID id for the cloud blob
                    Photo photo = new Photo();
                    photo.BlobID = info.Name;
                    photo.Site = site;
                    photo.Width = img.Width;
                    photo.Height = img.Height;
                    photo.FileName = relativeName;
                    photo.AdditionalExifProperties = new List<MetaDatum>();

                    PhotoRepository.Insert(photo);
                    bool portrait = false;

                    // walk the image properties and set the appropriate fields on the image for the various meta data types (EXIF)
                    int len = propItems.Length;
                    for (var i = 0; i < len; i++)
                    {
                        PropertyItem propItem = propItems[i];

                        switch (propItem.Id)
                        {
                            case 0x112:
                                portrait = BitConverter.ToUInt16(propItem.Value, 0) == 6 ? true : false;
                                
                                break;
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

                    // only generate the phocalstream image if it has not already been generated
                    if (File.Exists(Path.Combine(basePath, @"High.jpg")) == false)
                    {
                        // this is a dirty hack, figure out why the image isn't opening with the correct width and height
                        if (portrait)
                        {
                            photo.Width = img.Height;
                            photo.Height = img.Width;
                        }

                        ResizeImageTo(fileName, 1200, 800, Path.Combine(basePath, @"High.jpg"), portrait);
                        ResizeImageTo(fileName, 800, 533, Path.Combine(basePath, @"Medium.jpg"), portrait);
                        ResizeImageTo(fileName, 400, 266, Path.Combine(basePath, @"Low.jpg"), portrait);

                        // create a DeepZoom image creater to generate the tile set for each raw image
                        ImageCreator creator = new ImageCreator();
                        creator.TileFormat = Microsoft.DeepZoomTools.ImageFormat.Jpg;
                        creator.TileOverlap = 1;
                        creator.TileSize = 256;

                        string dziPath = Path.Combine(basePath, "Tiles.dzi");
                        try
                        {
                            creator.Create(fileName, dziPath); // create the DeepZoom tileset
                        }
                        catch (Exception e)
                        {
                            throw new Exception(String.Format("Error creating deep zoom tiles for file {0}: {1}", fileName, e.Message));
                        }
                    }
                    
                    return photo;
                }
            }
            catch (Exception e)
            {
                // this should be logged
                throw new Exception(string.Format("Exception processing photo {0}. Message: {1}", fileName, e.Message));
            }
        }

        public void ProcessCollection(Collection collection)
        {
            CameraSite site = collection.Site;
            CollectionCreator creator = new CollectionCreator();
            creator.TileFormat = Microsoft.DeepZoomTools.ImageFormat.Jpg;
            creator.TileOverlap = 1;
            creator.TileSize = 256;

            string rootDeepZoomPath = Path.Combine(ConfigurationManager.AppSettings["PhotoPath"], site.DirectoryName);

            List<string> files = new List<string>();
            using (SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand("select BlobID from Photos inner join CameraSites on CameraSites.ID = Photos.Site_ID where CameraSites.Name = @name", conn))
                {
                    command.Parameters.AddWithValue("@name", site.DirectoryName);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            files.Add(reader.GetString(0));
                        }
                    }
                }
            }
            files = files.Select(p => Path.Combine(rootDeepZoomPath, Path.Combine(string.Format(@"{0}.phocalstream", p), "Tiles.dzi"))).ToList<string>();
            creator.Create(files, Path.Combine(rootDeepZoomPath, "collection.dzc"));

            collection.Status = CollectionStatus.COMPLETE;
            CollectionRepository.Update(collection);
        }

        private void ResizeImageTo(string fileName, int width, int height, string destination, bool portrait)
        {
            using (System.Drawing.Image image = System.Drawing.Image.FromFile(fileName))
            {
                var iwidth = image.Width;
                var iheight = image.Height;
                if (portrait)
                {
                    // XOR swap
                    iwidth = iwidth + iheight;
                    iheight = iwidth - iheight;
                    iwidth = iwidth - iheight;

                    width = width + height;
                    height = width - height;
                    width = width - height;

                    image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                }

                //a holder for the result
                using (Bitmap result = new Bitmap(width, height))
                {
                    //set the resolutions the same to avoid cropping due to resolution differences
                    result.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                    //use a graphics object to draw the resized image into the bitmap
                    using (Graphics graphics = Graphics.FromImage(result))
                    {
                        //set the resize quality modes to high quality
                        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        //draw the image into the target bitmap
                        graphics.DrawImage(image, 0, 0, result.Width, result.Height);
                    }
                    result.Save(destination);
                }
            }
        }

        public void GeneratePivotManifest(CameraSite site)
        {
            string rootDeepZoomPath = Path.Combine(ConfigurationManager.AppSettings["PhotoPath"], site.DirectoryName);
            XmlDocument doc = PhotoRepo.CreatePivotCollectionForSite(site.ID);

            doc.Save(Path.Combine(rootDeepZoomPath, "site.cxml"));
        }

        public void GeneratePivotManifest(string collectionID, string photoList)
        {
            string rootPath = Path.Combine(ConfigurationManager.AppSettings["PhotoPath"], collectionID);
            XmlDocument doc = PhotoRepo.CreatePivotCollectionForList(collectionID, photoList);

            doc.Save(Path.Combine(rootPath, "site.cxml"));
        }
    }
}
