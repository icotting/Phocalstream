using Phocalstream_Service.Service;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Data.Model.View;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
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

        public XmlDocument CreatePivotCollectionForSite(long siteID)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string siteName = "";
                using (SqlCommand command = new SqlCommand(string.Format("select Name from CameraSites where ID = {0}", siteID), conn))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            siteName = (string)reader["Name"];
                        }
                    }
                }

                using (SqlCommand command = new SqlCommand(string.Format("select ID, Captured from Photos where Site_ID = {0}", siteID), conn))
                {
                    return CreatePivotDocument(siteName, command, null, CollectionType.SITE);
                }
            }
        }

        public XmlDocument CreatePivotCollectionForList(string collectionName, string photoList)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (SqlCommand command = new SqlCommand(string.Format("select ID, Captured, Site_ID from Photos where Photos.ID IN ({0})", photoList), conn))
                {
                    return CreatePivotDocument(collectionName, command, null, CollectionType.SEARCH);
                }
            }
        }

        public string GetCameraSiteName(long siteID)
        {
            string siteName = "";
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (SqlCommand command = new SqlCommand(string.Format("select ID, Name, DirectoryName from CameraSites where CameraSites.ID = {0}", siteID), conn))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            siteName = (string)reader["Name"];
                        }
                    }
                }
            }

            return siteName;
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

        private XmlDocument CreatePivotDocument(string collectionName, SqlCommand command, Uri uri, CollectionType type)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Collection");

            root.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            root.SetAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
            root.SetAttribute("xmlns:p", "http://schemas.microsoft.com/livelabs/pivot/collection/2009");
            root.SetAttribute("SchemaVersion", "1.0");
            root.SetAttribute("Name", string.Format("{0} Photo Collection", collectionName));

            root.SetAttribute("xmlns", "http://schemas.microsoft.com/collection/metadata/2009");
            doc.AppendChild(root);

            XmlDeclaration xmldecl = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            doc.InsertBefore(xmldecl, root);

            XmlElement facets = doc.CreateElement("FacetCategories");
            XmlElement facet = doc.CreateElement("FacetCategory");
            facet.SetAttribute("Name", "Date");
            facet.SetAttribute("Type", "DateTime");
            facet.SetAttribute("IsFilterVisible", "false");
            facets.AppendChild(facet);

            facet = doc.CreateElement("FacetCategory");
            facet.SetAttribute("Name", "Time of Day");
            facet.SetAttribute("Type", "String");
            facets.AppendChild(facet);

            facet = doc.CreateElement("FacetCategory");
            facet.SetAttribute("Name", "Time of Year");
            facet.SetAttribute("Type", "String");
            facets.AppendChild(facet);

            if (type == CollectionType.SEARCH)
            {
                facet = doc.CreateElement("FacetCategory");
                facet.SetAttribute("Name", "Site");
                facet.SetAttribute("Type", "String");
                facets.AppendChild(facet);
            }

            root.AppendChild(facets);

            string dzCollection;
            if (type == CollectionType.SEARCH)
            {
                dzCollection = (uri == null) ? string.Format("/dzc/{0}{1}/collection.dzc", PathManager.SearchPath, collectionName)
                    : string.Format("{0}://{1}:{2}/dzc/{3}{4}/collection.dzc", uri.Scheme,
                    uri.Host,
                    uri.Port,
                    PathManager.SearchPath,
                    collectionName);
            }
            else
            {
                dzCollection = (uri == null) ? string.Format("/dzc/{0}/collection.dzc", collectionName)
                    : string.Format("{0}://{1}:{2}/dzc/{3}/collection.dzc", uri.Scheme,
                    uri.Host,
                    uri.Port,
                    collectionName);
            }

            XmlElement items = doc.CreateElement("Items");
            items.SetAttribute("ImgBase", dzCollection);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                int position = 0;
                while (reader.Read())
                {
                    items.AppendChild(ItemFor(doc, reader, collectionName, (type != CollectionType.SITE), position++));
                }
            }

            root.AppendChild(items);
            return doc;
        }

        private XmlElement ItemFor(XmlDocument doc, SqlDataReader photo, string collectionName, bool includeSite, int position)
        {
            DateTime captured = (DateTime)photo["Captured"];

            string siteName;
            if(includeSite)
            {
                 siteName = GetCameraSiteName((long)photo["Site_ID"]);
            }
            else
            {
                siteName = collectionName;
            }

            XmlElement item = doc.CreateElement("Item");
            item.SetAttribute("Img", String.Format("#{0}", position));
            item.SetAttribute("Id", Convert.ToString(photo["ID"]));
            item.SetAttribute("Name", string.Format("{0} {1}", siteName, captured.ToString("MMM dd, yyyy hh:mm tt")));
            item.SetAttribute("Href", "http://www.google.com");

            XmlElement facets = doc.CreateElement("Facets");
            XmlElement facet = doc.CreateElement("Facet");
            facet.SetAttribute("Name", "Date");
            XmlElement facetValue = doc.CreateElement("DateTime");
            facetValue.SetAttribute("Value", captured.ToString("yyyy-MM-ddTHH:mm:ss"));
            facet.AppendChild(facetValue);
            facets.AppendChild(facet);

            int hour = captured.Hour;
            string timeOfDay = "";
            if (hour >= 21 || hour < 5)
            {
                timeOfDay = "Night";
            }
            else if (hour >= 5 && hour < 12)
            {
                timeOfDay = "Morning";
            }
            else if (hour >= 12 && hour < 17)
            {
                timeOfDay = "Afternoon";
            }
            else if (hour >= 17 && hour < 21)
            {
                timeOfDay = "Evening";
            }

            facet = doc.CreateElement("Facet");
            facet.SetAttribute("Name", "Time of Day");
            facetValue = doc.CreateElement("String");
            facetValue.SetAttribute("Value", timeOfDay);
            facet.AppendChild(facetValue);
            facets.AppendChild(facet);

            int month = captured.Month;
            string timeOfYear = "";
            if (month >= 12 || month < 3)
            {
                timeOfYear = "Winter";
            }
            else if (month >= 3 && month < 6)
            {
                timeOfYear = "Spring";
            }
            else if (month >= 6 && month < 9)
            {
                timeOfYear = "Summer";
            }
            else if (month >= 9 && month < 12)
            {
                timeOfYear = "Fall";
            }

            facet = doc.CreateElement("Facet");
            facet.SetAttribute("Name", "Time of Year");
            facetValue = doc.CreateElement("String");
            facetValue.SetAttribute("Value", timeOfYear);
            facet.AppendChild(facetValue);
            facets.AppendChild(facet);

            if (includeSite)
            {
                facet = doc.CreateElement("Facet");
                facet.SetAttribute("Name", "Site");
                facetValue = doc.CreateElement("String");
                facetValue.SetAttribute("Value", siteName);
                facet.AppendChild(facetValue);
                facets.AppendChild(facet);
            }

            item.AppendChild(facets);
            return item;
        }
    }
}