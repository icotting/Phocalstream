using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Data.Model.View;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Xml;

namespace Phocalstream_Web.Application.Data
{
    public class PhotoRepository : IPhotoRepository
    {
        private string _connectionString;

        public PhotoRepository(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("The photo repository requires an SQL connection");
            }
            _connectionString = connectionString;
        }

        public SiteDetails GetSiteDetails(CameraSite site)
        {
            SiteDetails details = new SiteDetails();
            details.SiteName = site.Name;
            details.SiteID = site.ID;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand("select min(Captured), max(Captured), count(*), max(ID) from Photos where Site_ID = @siteID", conn))
                {
                    command.Parameters.AddWithValue("@siteID", site.ID);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            details.First = reader.GetDateTime(0);
                            details.Last = reader.GetDateTime(1);
                            details.PhotoCount = reader.GetInt32(2);
                            details.LastPhotoID = reader.GetInt64(3);
                        }
                    }
                }
            }
            return details;
        }


        public System.Xml.XmlDocument CreateDeepZoomForSite(long siteID)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand("select Photos.ID,BlobID,Width,Height,ContainerID from Photos inner join CameraSites on Photos.Site_ID = CameraSites.ID where CameraSites.ID = @id", conn))
                {
                    command.Prepare();
                    command.Parameters.AddWithValue("@id", siteID);
                    return CreateDeepZoomDocument(command, null);
                }
            }
        }

        public System.Xml.XmlDocument CreateDeepZomForList(string photoList)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand("select Photos.ID,BlobID,Width,Height,ContainerID from Photos inner join CameraSites on Photos.Site_ID = CameraSites.ID where Photos.ID in (" + photoList + ")", conn))
                {
                    return CreateDeepZoomDocument(command, null);
                }
            }
        }

        public ICollection<TimelapseFrame> CreateFrameSet(string photoList, string urlScheme, string urlHost, int urlPort)
        {
            List<TimelapseFrame> frames = new List<TimelapseFrame>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "select BlobID, ContainerID, Captured, Photos.ID from Photos inner join CameraSites on Photos.Site_ID = CameraSites.ID where Photos.ID in (" + photoList + ")";
                conn.Open();
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TimelapseFrame frame = new TimelapseFrame();
                            frame.Time = reader.GetDateTime(2);
                            frame.PhotoId = reader.GetInt64(3);
                            frame.Url = string.Format("{0}://{1}:{2}/dzc/{3}/DZ/{4}.dzi", urlScheme,
                                urlHost,
                                Convert.ToString(urlPort),
                                reader.GetString(1),
                                reader.GetString(0));
                            frames.Add(frame);
                        }
                    }
                }
            }
            

            return frames;
        }

        private XmlDocument CreateDeepZoomDocument(SqlCommand command, Uri uri)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Collection");
            root.SetAttribute("MaxLevel", "7");
            root.SetAttribute("TileSize", "256");
            root.SetAttribute("Format", "jpg");
            root.SetAttribute("xmlns", "http://schemas.microsoft.com/deepzoom/2009");

            doc.AppendChild(root);

            XmlDeclaration xmldecl = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            doc.InsertBefore(xmldecl, root);

            XmlElement items = doc.CreateElement("Items");

            using (SqlDataReader reader = command.ExecuteReader())
            {
                int count = 0;
                while (reader.Read())
                {
                    string imageSource = ( uri == null ) ? string.Format("/dzc/{3}-dz/{4}.dzi", reader.GetString(4),reader.GetString(1))
                        : string.Format("{0}://{1}:{2}/dzc/{3}-dz/{4}.dzi", uri.Scheme,
                        uri.Host,
                        uri.Port,
                        reader.GetString(4),
                        reader.GetString(1));

                    XmlElement item = doc.CreateElement("I");
                    item.SetAttribute("Source", imageSource);
                    item.SetAttribute("N", Convert.ToString(count++));
                    item.SetAttribute("Id", Convert.ToString(reader.GetInt64(0)));

                    XmlElement size = doc.CreateElement("Size");
                    size.SetAttribute("Width", Convert.ToString(reader.GetInt32(2)));
                    size.SetAttribute("Height", Convert.ToString(reader.GetInt32(3)));
                    item.AppendChild(size);

                    items.AppendChild(item);
                }
                root.SetAttribute("NextItemId", Convert.ToString(count));
            }
            root.AppendChild(items);
            return doc;
        }
    }
}