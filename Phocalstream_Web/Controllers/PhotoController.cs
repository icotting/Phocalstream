using Microsoft.Practices.Unity;
using Phocalstream_Shared;
using Phocalstream_Shared.Data;
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
        [Dependency]
        public IEntityRepository<Photo> PhotoRepository { get; set; }

        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IDroughtMonitorRepository DmRepository { get; set; }

        public ActionResult Index(long photoID)
        {
            PhotoViewModel model = new PhotoViewModel();
            model.Photo = PhotoRepository.Single(p => p.ID == photoID, p => p.Site);
            
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
            model.Photo = PhotoRepository.Single(p => p.ID == photoID, p => p.Site);
            model.PhotoDate = model.Photo.Captured.ToString("MMM dd, yyyy");
            model.PhotoTime = model.Photo.Captured.ToString("h:mm:ss tt");

            return PartialView("_PhotoInfo", model);
        }

        public ActionResult CameraCollection(long siteID)
        {
            CollectionViewModel model = new CollectionViewModel();
            model.Collection = CollectionRepository.First(c => c.Site.ID == siteID);

            model.CollectionUrl = string.Format("{0}://{1}:{2}/api/sitecollection/pivotcollectionfor?id={3}", Request.Url.Scheme,
                Request.Url.Host,
                Request.Url.Port,
                model.Collection.ID);
            model.SiteCoords = string.Format("{0}, {1}", model.Collection.Site.Latitude, model.Collection.Site.Longitude);

            List<Photo> photos = PhotoRepository.Find(p => p.Site.ID == model.Collection.Site.ID).OrderBy(p => p.Captured).ToList<Photo>();
            model.SiteDetails = new SiteDetails() { PhotoCount = photos.Count(), First = photos.Select(p => p.Captured).First(), Last = photos.Select(p => p.Captured).Last() };
            
            return View(model);
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

            Photo photo = PhotoRepository.Single(p => p.ID == photoID);
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
            DroughtMonitorWeek week = null;
            switch (type)
            {
                case DMDataType.COUNTY:
                    week = DmRepository.FindBy(DmRepository.GetCountyForFips(CountyFIPS), date).FirstOrDefault();
                    break;
                case DMDataType.STATE:
                    week = DmRepository.FindBy(DmRepository.GetCountyForFips(CountyFIPS).State, date).FirstOrDefault();
                    break;
                case DMDataType.ALL:
                    week = DmRepository.FindUS(date).FirstOrDefault();
                    break;
            }

            if (week == null)
            {
                USCounty county = DmRepository.GetCountyForFips(CountyFIPS);
                week = new DroughtMonitorWeek() { 
                    D0 = 0, 
                    D1 = 0, 
                    D2 = 0, 
                    D3 = 0, 
                    D4 = 0, 
                    NonDrought = 0, 
                    Week = date, 
                    Type = type, 
                    County = county, 
                    State = county.State };
            }
            else
            {
                // Normalize data to be out of 100%
                week.D0 = (float)Math.Round((week.D0 - week.D1), 2);
                week.D1 = (float)Math.Round((week.D1 - week.D2), 2);
                week.D2 = (float)Math.Round((week.D2 - week.D3), 2);
                week.D3 = (float)Math.Round((week.D3 - week.D4), 2);
            }

            return week;
        } //End LoadDMDataValues
    }
}
