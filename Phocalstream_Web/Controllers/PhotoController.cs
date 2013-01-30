using Phocalstream_Shared;
using Phocalstream_Shared.Data.Model.External;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Data.Model.View;
using Phocalstream_Web.Application;
using Phocalstream_Web.Application.Data;
using Phocalstream_Web.Models;
using Phocalstream_Web.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
//using System.Xml;

namespace Phocalstream_Web.Controllers
{
    public class PhotoController : Controller
    {
        [AllowAnonymous]
        public ActionResult Index(long photoID)
        {
            PhotoViewModel model = new PhotoViewModel();
            using (ApplicationContext ctx = new ApplicationContext())
            {
                model.Photo = ctx.Photos.Include("Site").SingleOrDefault(p => p.ID == photoID);
            }

            if (model.Photo == null)
            {
                return new HttpNotFoundResult(string.Format("Photo {0} was not found", photoID));
            }

            model.ImageUrl = string.Format("{0}://{1}:{2}/dzc/{3}/DZ/{4}.dzi", Request.Url.Scheme,
                    Request.Url.Host,
                    Request.Url.Port,
                    model.Photo.Site.ContainerID,
                    model.Photo.BlobID);
            model.PhotoDate = model.Photo.Captured.ToString("MMM dd, yyyy");
            model.PhotoTime = model.Photo.Captured.ToString("h:mm:ss tt");
            model.SiteCoords = string.Format("{0}, {1}", model.Photo.Site.Latitude, model.Photo.Site.Longitude);

            model.DroughtMonitorData = LoadDMData(DMDataType.COUNTY, model.Photo.Captured, model.Photo.Site.CountyFips);
            model.DroughtMonitorData.PhotoID = photoID;

            return View(model);
        }

        public PartialViewResult PhotoDetails(long photoID)
        {
            PhotoViewModel model = new PhotoViewModel();
            using (ApplicationContext ctx = new ApplicationContext())
            {
                model.Photo = ctx.Photos.Include("Site").SingleOrDefault(p => p.ID == photoID);
                model.PhotoDate = model.Photo.Captured.ToString("MMM dd, yyyy");
                model.PhotoTime = model.Photo.Captured.ToString("h:mm:ss tt");
            }
            return PartialView("_PhotoInfo", model);
        }

        [AllowAnonymous]
        public ActionResult CameraCollection(long siteID)
        {
            using (ApplicationContext ctx = new ApplicationContext())
            {
                CollectionViewModel model = new CollectionViewModel();
                model.Collection = (from c in ctx.Collections where c.Site.ID == siteID select c).First();

                model.CollectionUrl = string.Format("{0}://{1}:{2}/api/sitecollection/pivotcollectionfor?id={3}", Request.Url.Scheme,
                    Request.Url.Host,
                    Request.Url.Port,
                    model.Collection.ID);
                model.SiteCoords = string.Format("{0}, {1}", model.Collection.Site.Latitude, model.Collection.Site.Longitude);

                List<Photo> photos = ctx.Photos.Where(p => p.Site.ID == model.Collection.Site.ID).OrderBy(p => p.Captured).ToList<Photo>();
                model.SiteDetails = new SiteDetails() { PhotoCount = photos.Count(), First = photos.Select(p => p.Captured).First(), Last = photos.Select(p => p.Captured).Last() };
                return View(model);
            }
        }

        [HttpGet]
        public ActionResult DroughtMonitorData(string type, long photoID)
        {
            DMDataType dmDataType = DMDataType.COUNTY;
            switch (type)
            {
                case "County":
                    dmDataType = DMDataType.COUNTY;
                    break;
                case "State":
                    dmDataType = DMDataType.STATE;
                    break;
                case "US":
                    dmDataType = DMDataType.US;
                    break;
            }

            Photo photo = null;
            using (ApplicationContext ctx = new ApplicationContext())
            {
                photo = ctx.Photos.Include("Site").SingleOrDefault(p => p.ID == photoID);
            }

            if (photo == null)
            {
                return new HttpNotFoundResult(string.Format("Photo {0} was not found", photoID));
            }

            DmData model = LoadDMData(dmDataType, photo.Captured, photo.Site.CountyFips);
            model.PhotoID = photoID;

            return PartialView("_DmPartial", model);
        } //End DroughtMonitorData

        private DmData LoadDMData(DMDataType type, DateTime date, int CountyFIPS)
        {
            DmData data = new DmData();
            data.DMValues = LoadDMDataValues(type, date, CountyFIPS);
            data.PreviousWeekValues = LoadDMDataValues(type, date.AddDays(-7), CountyFIPS);
            data.PreviousMonthValues = LoadDMDataValues(type, date.AddDays(-30), CountyFIPS);

            //data.DMValues.Type = type.ToString().ToLower();
            data.DisplayDate = data.DMValues.Week.ToString("MM-dd-yyyy");
            data.DataWeek = data.DMValues.Week.ToString("yyMMdd");

            return data;
        } //End Load DM Data


        private DroughtMonitorWeek LoadDMDataValues (DMDataType type, DateTime date, int CountyFIPS)
        {
            DroughtMonitorWeek DMValues = new DroughtMonitorWeek();

            //data.DMValues.Type = type.ToString().ToLower();
            DMValues.Type = type;
            DMValues.Week = DroughtMonitorWeek.ConvertDateToTuesday(date);

            // Get the ConnectionString by using the configuration ConnectionStrings property to read the connectionString. 
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DMConnection"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(null, conn))
                {
                    switch (type)
                    {
                        case DMDataType.COUNTY:
                            command.CommandText = string.Format("select DroughtCategory, DroughtValue from CountyDMValues where PublishedDate='{0}' and County_ID={1}", DMValues.Week.ToString("yyyyMMdd"), GetCountyID(conn, CountyFIPS));
                            break;
                        case DMDataType.STATE:
                            command.CommandText = string.Format("select DroughtCategory, DroughtValue from StateDMValues where PublishedDate='{0}' and State_ID={1}", DMValues.Week.ToString("yyyyMMdd"), GetStateID(conn, CountyFIPS));
                            break;
                        case DMDataType.US:
                            command.CommandText = string.Format("select DroughtCategory, DroughtValue from USDMValues where PublishedDate='{0}'", DMValues.Week.ToString("yyyyMMdd"));
                            break;
                    }

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            switch (reader.GetInt32(0))
                            {
                                case 0:
                                    DMValues.NonDrought = (float)reader.GetDouble(1);
                                    break;
                                case 1:
                                    DMValues.D0 = (float)reader.GetDouble(1);
                                    break;
                                case 2:
                                    DMValues.D1 = (float)reader.GetDouble(1);
                                    break;
                                case 3:
                                    DMValues.D2 = (float)reader.GetDouble(1);
                                    break;
                                case 4:
                                    DMValues.D3 = (float)reader.GetDouble(1);
                                    break;
                                case 5:
                                    DMValues.D4 = (float)reader.GetDouble(1);
                                    break;
                            }
                        }
                        reader.Close();
                    }
                }
            }

            // Normalize data to be out of 100%
            DMValues.D0 = (float) Math.Round((DMValues.D0 - DMValues.D1), 2);
            DMValues.D1 = (float) Math.Round((DMValues.D1 - DMValues.D2), 2);
            DMValues.D2 = (float) Math.Round((DMValues.D2 - DMValues.D3), 2);
            DMValues.D3 = (float) Math.Round((DMValues.D3 - DMValues.D4), 2);

            return DMValues;
        } //End LoadDMDataValues

        private long GetCountyID(SqlConnection conn, int countyFIPS)
        {
            long countyID = -1;
            using (SqlCommand countyLookup = new SqlCommand(string.Format("select ID from Counties where FIPS='{0}'", countyFIPS), conn))
            using (SqlDataReader reader = countyLookup.ExecuteReader())
            {
                if (reader.Read())
                {
                    countyID = reader.GetInt64(0);
                }
                reader.Close();
            }
            return countyID;
        } //End GetCountyID

        private long GetStateID(SqlConnection conn, int countyFIPS)
        {
            long stateID = -1;
            using (SqlCommand countyLookup = new SqlCommand(string.Format("select State_ID from Counties where FIPS='{0}'", countyFIPS), conn))
            using (SqlDataReader reader = countyLookup.ExecuteReader())
            {
                if (reader.Read())
                {
                    stateID = reader.GetInt64(0);
                }
                reader.Close();
            }
            return stateID;
        } //End GetStateID
    }
}
