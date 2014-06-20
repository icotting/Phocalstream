﻿using Ionic.Zip;
using Microsoft.Practices.Unity;
using Phocalstream_Service.Service;
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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Xml.Serialization;
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

        [Dependency]
        public IWaterDataRepository WaterRepository { get; set; }

        [Dependency]
        public IPhotoRepository DZPhotoRepository { get; set; }

        [Dependency]
        public IEntityRepository<User> UserRepository { get; set; }

        public ActionResult Index(long photoID)
        {
            PhotoViewModel model = new PhotoViewModel();
            model.Photo = PhotoRepository.Single(p => p.ID == photoID, p => p.Site);
            
            if (model.Photo == null)
            {
                return new HttpNotFoundResult(string.Format("Photo {0} was not found", photoID));
            }

            model.ImageUrl = string.Format("{0}://{1}:{2}/dzc/{3}/{4}.phocalstream/Tiles.dzi", Request.Url.Scheme,
                    Request.Url.Host,
                    Request.Url.Port,
                    model.Photo.Site.Name,
                    model.Photo.BlobID);

            model.PhotoDate = model.Photo.Captured.ToString("MMM dd, yyyy");
            model.PhotoTime = model.Photo.Captured.ToString("h:mm:ss tt");
            model.SiteCoords = string.Format("{0}, {1}", model.Photo.Site.Latitude, model.Photo.Site.Longitude);

            model.DroughtMonitorData = LoadDMData(DMDataType.COUNTY, model.Photo.Captured, model.Photo.Site.CountyFips);
            model.DroughtMonitorData.PhotoID = photoID;

            model.WaterData = LoadWaterData(model.Photo.Site.Latitude, model.Photo.Site.Longitude, model.Photo.Captured);
            model.WaterData.PhotoID = photoID;

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

        private WaterFlowData LoadWaterData(double siteLat, double siteLong, DateTime date)
        {
            WaterFlowData data = new WaterFlowData();
            data.DataTypes = WaterRepository.FetchBestDataTypesForStationDate(WaterRepository.GetClosestStations(siteLat, siteLong, 1), date).ElementAt(0);
            data.WaterDataValues = WaterRepository.FetchByDateRange(data.DataTypes.StationID, data.DataTypes.DataID, date.AddDays(-42), date);
            data.ParameterInfo = WaterRepository.GetParameterCodeInfoFromDataType(data.DataTypes.DataID);
            data.ClosestStation = WaterRepository.GetStationInfo(data.DataTypes.StationID);
            
            data.chartDataValues = "";
            foreach (WaterDataValue value in data.WaterDataValues)
            {
                if (value.Value == -999999)
                {
                    data.chartDataValues += "null, ";
                }
                else
                {
                    data.chartDataValues += value.Value + ", ";
                }

            }
            data.chartDataValues = data.chartDataValues.Substring(0, data.chartDataValues.Length - 2);

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
                case DMDataType.US:
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
                week.Type = type;
                // Normalize data to be out of 100%
                week.D0 = (float)Math.Round((week.D0 - week.D1), 2);
                week.D1 = (float)Math.Round((week.D1 - week.D2), 2);
                week.D2 = (float)Math.Round((week.D2 - week.D3), 2);
                week.D3 = (float)Math.Round((week.D3 - week.D4), 2);
            }

            return week;
        } //End LoadDMDataValues

        [HttpPost]
        public ActionResult TimeLapse(string photoIds)
        {
            TimelapseModel model = new TimelapseModel();

            List<TimelapseFrame> frames = DZPhotoRepository.CreateFrameSet(photoIds, Request.Url.Scheme, Request.Url.Host, Request.Url.Port).ToList<TimelapseFrame>();
            frames.OrderBy(f => f.Time);
            model.Ids = frames.Select(f => f.PhotoId).ToList<long>();
            model.Video = new TimelapseVideo() { Frames = frames };

            XmlSerializer serializer = new XmlSerializer(typeof(TimelapseVideo));
            MemoryStream stream = new MemoryStream();
            serializer.Serialize(stream, model.Video);
            stream.Seek(0, SeekOrigin.Begin);
            model.EncodedFrames = Convert.ToBase64String(stream.ToArray());

            return View(model);
        }

        public ActionResult DeleteDownload(string fileName)
        {
            if (fileName.Equals("ALL"))
            {
                FileInfo[] files = new DirectoryInfo(ConfigurationManager.AppSettings["downloadPath"]).GetFiles();
                foreach (var file in files)
                {
                    file.Delete();
                }
            }

            else
            {
                string filePath = ConfigurationManager.AppSettings["downloadPath"] + fileName;

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            return RedirectToAction("Downloads", "Home");
        }

        public void Download(string fileName)
        {
            System.IO.Stream iStream = null;

            // Buffer to read 10K bytes in chunk:
            byte[] buffer = new Byte[10000];

            // Length of the file:
            int length;

            // Total bytes to read:
            long dataToRead;

            // Identify the file to download including its path.
            string downloadPath = ConfigurationManager.AppSettings["downloadPath"] + fileName;

            try
            {
                // Open the file.
                iStream = new System.IO.FileStream(downloadPath, System.IO.FileMode.Open,
                            System.IO.FileAccess.Read, System.IO.FileShare.Read);


                // Total bytes to read:
                dataToRead = iStream.Length;

                Response.Clear();
                Response.ContentType = "application/octet-stream";
                Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
                Response.AddHeader("Content-Length", iStream.Length.ToString());

                // Read the bytes.
                while (dataToRead > 0)
                {
                    // Verify that the client is connected.
                    if (Response.IsClientConnected)
                    {
                        // Read the data in buffer.
                        length = iStream.Read(buffer, 0, 10000);

                        // Write the data to the current output stream.
                        Response.OutputStream.Write(buffer, 0, length);

                        // Flush the data to the output.
                        Response.Flush();

                        buffer = new Byte[10000];
                        dataToRead = dataToRead - length;
                    }
                    else
                    {
                        //prevent infinite loop if user disconnects
                        dataToRead = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
            finally
            {
                if (iStream != null)
                {
                    //Close the file.
                    iStream.Close();
                }
                Response.Close();
            }
        }
    }
}