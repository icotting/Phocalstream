using Phocalstream_Shared;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Web.Application;
using Phocalstream_Web.Application.Data;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Xml;

namespace Phocalstream_Web.Controllers.Api
{
    public class SiteCollectionController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage DeepZoomCollectionForSite(long siteID)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand("select Photos.ID,BlobID,Width,Height,ContainerID from Photos inner join CameraSites on Photos.Site_ID = CameraSites.ID where CameraSites.ID = @id", conn))
                {
                    command.Prepare();
                    command.Parameters.AddWithValue("@id", siteID);
                    HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
                    XmlDocument doc = CreateDeepZoomDocument(command);
                    MemoryStream stream = new MemoryStream();
                    doc.Save(stream);
                    stream.Position = 0;
                    message.Content = new StreamContent(stream);

                    message.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");

                    return message;
                }
            }
        }

        [HttpGet]
        public HttpResponseMessage DeepZoomCollection(string photoList)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand("select Photos.ID,BlobID,Width,Height,ContainerID from Photos inner join CameraSites on Photos.Site_ID = CameraSites.ID where Photos.ID in (" + photoList + ")", conn))
                {
                    HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
                    XmlDocument doc = CreateDeepZoomDocument(command);
                    MemoryStream stream = new MemoryStream();
                    doc.Save(stream);
                    stream.Position = 0;
                    message.Content = new StreamContent(stream);

                    message.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");

                    return message;
                }
            }
        }
  
        [HttpGet]
        public HttpResponseMessage PivotCollectionFor(int id)
        {
            using (ApplicationContext ctx = new ApplicationContext())
            {
                Collection collection = ctx.Collections.Include("Photos").SingleOrDefault<Collection>(c => c.ID == id);
                if (collection == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);

                XmlDocument doc = new XmlDocument();
                XmlElement root = doc.CreateElement("Collection");

                root.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                root.SetAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
                root.SetAttribute("xmlns:p", "http://schemas.microsoft.com/livelabs/pivot/collection/2009");
                root.SetAttribute("SchemaVersion", "1.0");
                if (collection.Site != null)
                {
                    root.SetAttribute("Name", string.Format("{0} Photo Collection", collection.Site.Name));
                }
                else
                {
                    root.SetAttribute("Name", string.Format("{0} Photo Collection", collection.Name));
                }
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
                    root.AppendChild(facets);

                    if (collection.Type != CollectionType.SITE)
                    {
                        facet = doc.CreateElement("FacetCategory");
                        facet.SetAttribute("Name", "Site");
                        facet.SetAttribute("Type", "String");
                        facets.AppendChild(facet);
                        root.AppendChild(facets);
                    }

                string dzCollection = string.Format("{0}://{1}:{2}/dzc/{3}/DZ/collection.dzc", Request.RequestUri.Scheme,
                    Request.RequestUri.Host,
                    Request.RequestUri.Port,
                    collection.ContainerID);

                XmlElement items = doc.CreateElement("Items");
                items.SetAttribute("ImgBase", dzCollection);

                foreach (Photo photo in collection.Photos)
                {
                    items.AppendChild(ItemFor(doc, photo, (collection.Type != CollectionType.SITE)));
                }

                root.AppendChild(items);

                MemoryStream stream = new MemoryStream();
                doc.Save(stream);
                stream.Position = 0;
                message.Content = new StreamContent(stream);

                message.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
                return message;
            }
        }

        [NonAction]
        private XmlElement ItemFor(XmlDocument doc, Photo photo, bool includeSite)
        {
            XmlElement item = doc.CreateElement("Item");
            item.SetAttribute("Img", String.Format("#{0}", photo.ID));
            item.SetAttribute("Id", Convert.ToString(photo.ID));
            item.SetAttribute("Name", string.Format("{0} {1}", photo.Site.Name, photo.Captured.ToString("MMM dd, yyyy hh:mm tt")));
            item.SetAttribute("Href", "http://www.google.com");

            XmlElement facets = doc.CreateElement("Facets");
            XmlElement facet = doc.CreateElement("Facet");
            facet.SetAttribute("Name", "Date"); 
            XmlElement facetValue = doc.CreateElement("DateTime");
            facetValue.SetAttribute("Value", photo.Captured.ToString("yyyy-MM-ddTHH:mm:ss"));
            facet.AppendChild(facetValue);
            facets.AppendChild(facet);

            int hour = photo.Captured.Hour;
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

            int month = photo.Captured.Month;
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
                facetValue.SetAttribute("Value", photo.Site.Name);
                facet.AppendChild(facetValue);
                facets.AppendChild(facet);
            }

            item.AppendChild(facets);
            return item;
        }

        [NonAction]
        private XmlDocument CreateDeepZoomDocument(SqlCommand command)
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
                    string imageSource = string.Format("{0}://{1}:{2}/dzc/{3}-dz/{4}.dzi", Request.RequestUri.Scheme,
                        Request.RequestUri.Host,
                        Request.RequestUri.Port,
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
