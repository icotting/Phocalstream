using Phocalstream_Shared;
using Phocalstream_Shared.Models;
using Phocalstream_Web.Application;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
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
        // GET api/collection/5
        [AllowAnonymous]
        public HttpResponseMessage Get(int id)
        {
            using (EntityContext ctx = new EntityContext())
            {
                CameraSite site = ctx.Sites.Find(id);
                if (site == null)
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
                root.SetAttribute("Name", string.Format("{0} Photo Collection", site.Name));
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

                    string dzCollection = string.Format("{0}://{1}:{2}/dzc/{3}-dz/da.dzc", Request.RequestUri.Scheme, 
                        Request.RequestUri.Host, 
                        Request.RequestUri.Port, 
                        site.ContainerID);

                XmlElement items = doc.CreateElement("Items");
                items.SetAttribute("ImgBase", string.Format("/dzc/{0}-dz/da.dzc", site.ContainerID));

                    XmlElement item = null;
                    // to get the photo meta data for the DeepZoom referenced image, the photo will need to be looked up by BlobID (not ideal)
                    using (XmlReader collectionReader = XmlReader.Create(dzCollection))
                    {
                        int i = 0;
                        while (collectionReader.Read())
                        {
                            if (collectionReader.NodeType == XmlNodeType.Element && collectionReader.Name == "I")
                            {
                                items.AppendChild(GetItemFor(doc, ctx, 
                                    collectionReader.GetAttribute("Source").Substring(0, collectionReader.GetAttribute("Source").IndexOf(".")), 
                                    collectionReader.GetAttribute("Id")));
                            }
                        }
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

        private XmlElement GetItemFor(XmlDocument doc, EntityContext context, string BlobID, string Id)
        {
            Photo photo = (from p in context.Photos where p.BlobID == BlobID select p).First<Photo>();

            XmlElement item = doc.CreateElement("Item");
            item.SetAttribute("Img", String.Format("#{0}", Id));
            item.SetAttribute("Id", Convert.ToString(photo.ID));
            item.SetAttribute("Name", string.Format("{0} {1}", photo.Site.Name, photo.Captured.ToString("MMM dd, yyyy hh:mm tt")));
            
            XmlElement facets = doc.CreateElement("Facets");
            XmlElement facet = doc.CreateElement("Facet");
            facet.SetAttribute("Name", "Date"); 
            XmlElement facetValue = doc.CreateElement("DateTime");
            facetValue.SetAttribute("Value", photo.Captured.ToString("yyyy-MM-ddThh:mm:ss"));
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

            item.AppendChild(facets);
            return item;
        }
    }
}
