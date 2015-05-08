using Microsoft.Practices.Unity;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.External;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Service;
using Phocalstream_Web.Application;
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
        public IEntityRepository<Tag> TagRepository { get; set; }

        [Dependency]
        public IEntityRepository<CameraSite> SiteRepository { get; set; }

        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IDroughtMonitorRepository DMRepository { get; set; }

        [Dependency]
        public IPhotoRepository PhotoRepo { get; set; }

        [Dependency]
        public IUnitOfWork Unit { get; set; }

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

        public void FinishCollectionProcessing(Collection collection)
        {
            collection.Status = CollectionStatus.COMPLETE;
            CollectionRepository.Update(collection);
        }

        public Phocalstream_Shared.Data.Model.Photo.Photo ProcessPhoto(string fileName, CameraSite site)
        {
            string relativeName = fileName;
            fileName = Path.Combine(PathManager.GetRawPath(), fileName);
            FileInfo info = new FileInfo(fileName);
            
            try
            {
                // create the directory for the image and its components
                string basePath = Path.Combine(Path.Combine(PathManager.GetPhotoPath(), site.DirectoryName), string.Format("{0}.phocalstream", info.Name));
                if (Directory.Exists(basePath) == false)
                {
                    Directory.CreateDirectory(basePath);
                }

                // open a Bitmap for the image to parse the meta data from
                using (System.Drawing.Image img = System.Drawing.Image.FromFile(fileName))
                {
                    Photo photo = CreatePhotoWithProperties(img, info.Name);
                    photo.Site = site;
                    photo.FileName = relativeName;

                    PhotoRepository.Insert(photo);

                    // only generate the phocalstream image if it has not already been generated
                    if (File.Exists(Path.Combine(basePath, @"High.jpg")) == false)
                    {
                        // this is a dirty hack, figure out why the image isn't opening with the correct width and height
                        if (photo.Portrait)
                        {
                            photo.Width = img.Height;
                            photo.Height = img.Width;
                        }

                        ResizeImageTo(fileName, 1200, 800, Path.Combine(basePath, @"High.jpg"), photo.Portrait);
                        ResizeImageTo(fileName, 800, 533, Path.Combine(basePath, @"Medium.jpg"), photo.Portrait);
                        ResizeImageTo(fileName, 400, 266, Path.Combine(basePath, @"Low.jpg"), photo.Portrait);
                    }

                    float[] percentages = ConvertCountsToPercentage(CountRGBPixels(new Bitmap(img)));
                    photo.Black = percentages[(int)PixelColor.BLACK];
                    photo.White = percentages[(int)PixelColor.WHITE];
                    photo.Red = percentages[(int)PixelColor.RED];
                    photo.Green = percentages[(int)PixelColor.GREEN];
                    photo.Blue = percentages[(int)PixelColor.BLUE];
                    
                    return photo;
                }
            }
            catch (Exception e)
            {
                // this should be logged
                throw new Exception(string.Format("Exception processing photo {0}. Message: {1}", fileName, e.Message));
            }
        }

        public Photo ProcessUserPhoto(Stream stream, string fileName, User user, long collectionID)
        {
            Collection collection = CollectionRepository.Find(c => c.ID == collectionID && c.Type == CollectionType.USER, c => c.Site, c => c.Photos).FirstOrDefault();

            string userFolder = Path.Combine(PathManager.GetUserCollectionPath(), Convert.ToString(user.ID));
            if (!Directory.Exists(userFolder))
            {
                Directory.CreateDirectory(userFolder);
            }

            try
            {
                // create the directory for the image and its components
                string basePath = Path.Combine(userFolder, collection.ContainerID, string.Format("{0}.phocalstream", fileName));
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }

                // open a Bitmap for the image to parse the meta data from
                using (System.Drawing.Image img = System.Drawing.Image.FromStream(stream))
                {
                    string savePath = Path.Combine(basePath, fileName);
                    img.Save(savePath);

                    Photo photo = CreatePhotoWithProperties(img, fileName);
                    photo.FileName = fileName;
                    photo.Site = collection.Site;
                    
                    PhotoRepository.Insert(photo);

                    collection.Photos.Add(photo);
                    collection.Status = CollectionStatus.INVALID;

                    Unit.Commit();

                    // only generate the phocalstream image if it has not already been generated
                    if (File.Exists(Path.Combine(basePath, @"High.jpg")) == false)
                    {
                        // this is a dirty hack, figure out why the image isn't opening with the correct width and height
                        if (photo.Portrait)
                        {
                            photo.Width = img.Height;
                            photo.Height = img.Width;
                        }

                        ResizeImageTo(savePath, 1200, 800, Path.Combine(basePath, @"High.jpg"), photo.Portrait);
                        ResizeImageTo(savePath, 800, 533, Path.Combine(basePath, @"Medium.jpg"), photo.Portrait);
                        ResizeImageTo(savePath, 400, 266, Path.Combine(basePath, @"Low.jpg"), photo.Portrait);
                    }
                    return photo;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Exception processing photo {0}. Message: {1}", fileName, e.Message));
            }
        }

        public Photo ProcessRGBForExistingPhoto(long photoID)
        {
            Photo photo = PhotoRepository.Single(p => p.ID == photoID);
            if (photo == null)
            {
                return null;
            }

            using (System.Drawing.Bitmap bitmap = new Bitmap(System.Drawing.Image.FromFile(string.Format("{0}{1}", PathManager.GetRawPath(), photo.FileName))))
            {
                int[] counts = CountRGBPixels(bitmap);
                float[] percentages = ConvertCountsToPercentage(counts);

                photo.Black = percentages[(int)PixelColor.BLACK];
                photo.White = percentages[(int)PixelColor.WHITE];
                photo.Red = percentages[(int)PixelColor.RED];
                photo.Green = percentages[(int)PixelColor.GREEN];
                photo.Blue = percentages[(int)PixelColor.BLUE];

                return photo;
            }
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

        private int[] CountRGBPixels(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            var counts = new int[] { 0, 0, 0, 0, 0 };
                
            Color pixel;
            PixelColor value;
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    pixel = bitmap.GetPixel(x, y);
                    value = ComputePixelColor(pixel);
                    counts[(int)value] += 1;
                }
            }

            return counts;
        }

        private PixelColor ComputePixelColor(Color pixel)
        {
            byte r = pixel.R;
            byte g = pixel.G;
            byte b = pixel.B;

            // BLACK or WHITE
            if (r == g && g == b && b == r)
            {
                if (r < 128)
                {
                    return PixelColor.BLACK;
                }
                else
                {
                    return PixelColor.WHITE;
                }
            }
            else
            {
                if (r > g && r > b)
                {
                    return PixelColor.RED;
                }
                else if (g > b)
                {
                    return PixelColor.GREEN;
                }
                else
                {
                    return PixelColor.BLUE;
                }
            }
        }

        private float[] ConvertCountsToPercentage(int[] counts)
        {
            float max = 0;
            foreach (var c in counts)
            {
                max += c;
            }

            float[] percentages = new float[counts.Length];
            for (var i = 0; i < counts.Length; i++)
            {
                percentages[i] = ((float) counts[i]) / max;
            }

            return percentages;
        }
   
        private Photo CreatePhotoWithProperties(System.Drawing.Image img, string name)
        {
            // get image mea data
            PropertyItem[] propItems = img.PropertyItems;

            // create a new photo with a GUID id for the cloud blob
            Photo photo = new Photo();
            photo.BlobID = name;
            photo.Width = img.Width;
            photo.Height = img.Height;
            photo.AdditionalExifProperties = new List<MetaDatum>();

            photo.Portrait = false;

            // walk the image properties and set the appropriate fields on the image for the various meta data types (EXIF)
            int len = propItems.Length;
            for (var i = 0; i < len; i++)
            {
                PropertyItem propItem = propItems[i];

                switch (propItem.Id)
                {
                    case 0x112:
                        photo.Portrait = BitConverter.ToUInt16(propItem.Value, 0) == 6 ? true : false;
                                
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

            return photo;
        }

        public List<string> GetUnusedTagNames(long photoID)
        {
            var photoTags = PhotoRepository.Single(p => p.ID == photoID, p => p.Tags).Tags.Select(t => t.Name);
            return TagRepository.GetAll().Select(t => t.Name).Except(photoTags).ToList();
        }

        public List<string> GetTagNames()
        {
            return TagRepository.Find(t => !t.Name.Equals("")).Select(t => t.Name).ToList<string>();
        }

        public Photo AddTag(long photoID, string tags)
        {
            //Get the photo to be tagged
            Photo photo = PhotoRepository.Single(p => p.ID == photoID, p => p.Site, p => p.Tags);
            if (photo == null)
            {
                return null;
            }

            //Create the array of tags
            string[] tagArray = tags.Split(',');

            foreach (string name in tagArray)
            {
                //all tags are stored in lowercase
                String text = name.ToLower(); ;

                //Need to check if the tag exists
                Tag tag = TagRepository.Find(t => t.Name.Equals(text)).FirstOrDefault();

                //if tag is null, create one
                if (tag == null)
                {
                    tag = new Tag(name);
                }

                //add the tag
                photo.Tags.Add(tag);
            }

            //commit changes
            Unit.Commit();

            photo.AvailableTags = GetUnusedTagNames(photoID);

            return photo;
        }

        public List<Tuple<string, int, long>> GetPopularTagsForSite(long siteID)
        {
            List<Tuple<string, int, long>> PopularTags = new List<Tuple<string, int, long>>();
            using (SqlConnection conn = new SqlConnection(PathManager.DbConnection))
            {
                conn.Open();
                string commandString = "select Tags.Name, MAX(Photos.ID), Count(*) from Tags " + 
                        "INNER JOIN PhotoTags ON Tags.ID = PhotoTags.Tag_ID " +
                        "INNER JOIN Photos ON PhotoTags.Photo_ID = Photos.ID " +
                        "WHERE Photos.Site_ID = @siteID " +
                        "AND Tags.Name <> '' " +
                        "GROUP BY Tags.Name";
                using (SqlCommand command = new SqlCommand(commandString, conn))
                {
                    command.Parameters.AddWithValue("@siteID", siteID);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Tuple<string, int, long> tuple = new Tuple<string, int, long>(
                                reader.GetString(0),
                                reader.GetInt32(2),
                                reader.GetInt64(1)
                            );

                            PopularTags.Add(tuple);
                        }
                    }
                }
            }

            return PopularTags;
        }

        public List<string> GetFileNames(List<Photo> photos)
        {
            List<string> fileNames = new List<string>();
            
            foreach (Photo photo in photos)
            {
                Collection collection = CollectionRepository.Find(c => c.Site.ID == photo.Site.ID, c => c.Owner).FirstOrDefault();

                if (collection.Type == CollectionType.SITE)
                {
                    fileNames.Add(Path.Combine(PathManager.GetPhotoPath(), photo.Site.DirectoryName,
                        string.Format("{0}.phocalstream", photo.BlobID), "Tiles.dzi"));
                }
                else if (collection.Type == CollectionType.USER)
                {
                    fileNames.Add(Path.Combine(PathManager.GetUserCollectionPath(), Convert.ToString(collection.Owner.ID), photo.Site.DirectoryName,
                        string.Format("{0}.phocalstream", photo.BlobID), "Tiles.dzi"));
                }
            }

            return fileNames;
        }

        public ICollection<TimeLapseFrame> CreateTimeLapseFramesFromIDs(long[] photoIDs)
        {
            List<TimeLapseFrame> frames = new List<TimeLapseFrame>();
            long firstID = photoIDs[0]; // stupid LINQ

            USCounty county = DMRepository.GetCountyForFips(PhotoRepository.Find(p => p.ID == firstID, p => p.Site)
                .FirstOrDefault().Site.CountyFips);

            var properties = PhotoRepo.GetPhotoProperties(photoIDs, new string[] { "ID", "Captured" });
            foreach (var property in properties)
            {
                var frame = new TimeLapseFrame() { FrameTime = (DateTime)property["Captured"], PhotoID = (long)property["ID"] };
                frames.Add(frame);
            }

            return frames;
        }
    }
}
