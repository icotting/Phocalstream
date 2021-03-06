﻿using Ionic.Zip;
using Microsoft.Practices.Unity;
using Phocalstream_Service.Service;
using Phocalstream_Shared;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.External;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Data.Model.View;
using Phocalstream_Shared.Model.View;
using Phocalstream_Shared.Service;
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
        public IEntityRepository<Tag> TagRepository { get; set; }
        
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

        [Dependency]
        public IPhotoService PhotoService { get; set; }

        [Dependency]
        public ICollectionService CollectionService { get; set; }

        
        public ActionResult Index(long photoID)
        {
            PhotoViewModel model = new PhotoViewModel();
            model.Photo = PhotoRepository.Single(p => p.ID == photoID, p => p.Site, p => p.Tags);
            
            if (model.Photo == null)
            {
                return new HttpNotFoundResult(string.Format("Photo {0} was not found", photoID));
            }

            model.Photo.AvailableTags = PhotoService.GetUnusedTagNames(photoID);

            model.PhotoDate = model.Photo.Captured.ToString("MMM dd, yyyy");
            model.PhotoTime = model.Photo.Captured.ToString("h:mm:ss tt");
            model.SiteCoords = string.Format("{0}, {1}", model.Photo.Site.Latitude, model.Photo.Site.Longitude);

            model.DroughtMonitorData = LoadDMData(DMDataType.COUNTY, model.Photo.Captured, model.Photo.Site.CountyFips);
            model.DroughtMonitorData.PhotoID = photoID;

            model.WaterData = LoadWaterData(model.Photo.Site.Latitude, model.Photo.Site.Longitude, model.Photo.Captured);
            model.WaterData.PhotoID = photoID;

            model.UserCollections = LoadUserCollections(photoID);
            
            return View(model);
        }

        public ActionResult SiteDashboard(long siteID)
        {
            // Make sure the siteID belongs to a CameraSite
            if (CollectionRepository.Find(c => c.Site.ID == siteID && c.Type == CollectionType.SITE).FirstOrDefault() == null)
            {
                return RedirectToAction("SiteList", "Home", null);
            }
            else
            {
                SiteDashboardViewModel model = new SiteDashboardViewModel();

                model.CollectionViewModel = GetCollectionViewModel(siteID, -1);
                model.Years = GetSiteYearSummary(siteID);
                model.Tags = PhotoService.GetPopularTagsForSite(siteID);

                //Photo Frequency
                model.PhotoFrequency = GetPhotoFrequencyData(siteID);
                model.PhotoFrequency.SiteName = model.CollectionViewModel.Collection.Site.Name;
                model.PhotoFrequency.StartDate = model.CollectionViewModel.SiteDetails.First;

                Photo lastPhoto = PhotoRepository.Find(p => p.Site.ID == siteID).OrderBy(p => p.Captured).Last();
                DateTime lastPhotoDate = lastPhoto.Captured;

                model.DroughtMonitorData = LoadDMData(DMDataType.COUNTY, lastPhotoDate, model.CollectionViewModel.Collection.Site.CountyFips);
                model.DroughtMonitorData.PhotoID = lastPhoto.ID;

                model.WaterData = LoadWaterData(model.CollectionViewModel.Collection.Site.Latitude, model.CollectionViewModel.Collection.Site.Longitude, lastPhotoDate);
                model.WaterData.PhotoID = lastPhoto.ID;

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

            Photo photo = PhotoRepository.Single(p => p.ID == photoID);
            if (photo == null)
            {
                return new HttpNotFoundResult(string.Format("Photo {0} was not found", photoID));
            }

            DmData model = LoadDMData(dmDataType, photo.Captured, photo.Site.CountyFips);
            model.PhotoID = photoID;

            return PartialView("_DmPartial", model);
        } //End DroughtMonitorData

        [HttpGet]
        public ActionResult Timelapse(long collectionId)
        {
            TimelapseModel model = new TimelapseModel();
            Collection collection = CollectionRepository.Find(c => c.ID == collectionId, c => c.Photos, c => c.Site).FirstOrDefault();

            if (collection != null)
            {
                model.CollectionId = collection.ID; 
                model.Name = !String.IsNullOrWhiteSpace(collection.Name) ? collection.Name : "Dynamic Timelapse";
               
                long[] ids;
                switch (collection.Type)
                {
                    case CollectionType.SITE:
                        ids = PhotoRepository.Find(p => p.Site.ID == collection.Site.ID).Select(p => p.ID).ToArray<long>();
                        break;
                    case CollectionType.USER: 
                        ids = collection.Photos.Select(p => p.ID).ToArray<long>();
                        break;
                    case CollectionType.SEARCH: 
                        ids = collection.Photos.Select(p => p.ID).ToArray<long>();
                        break;
                    case CollectionType.TIMELAPSE:
                        ids = collection.Photos.Select(p => p.ID).ToArray<long>();
                        break;
                    default:
                        ids = new long[0];
                        break;
                }

                if (ids.Length > 0)
                {
                    model.DmWeeks = DZPhotoRepository.FindDmDatesForPhotos(ids);
                    model.Frames = PhotoService.CreateTimeLapseFramesFromIDs(ids);

                    long id = model.Frames.FirstOrDefault().PhotoID;

                    Photo first = PhotoRepository.Find(p => p.ID == id, p => p.Site).FirstOrDefault();
                    model.CountyFips = first.Site.CountyFips;
                    model.Latitude = first.Site.Latitude;
                    model.Longitude = first.Site.Longitude;

                    model.Width = first.Width;
                    model.Height = first.Height;

                    return View(model);
                }
            }

            return RedirectToAction("Index", "Search", null);
        }

        [HttpPost]
        public ActionResult TimeLapse(string photoIds, string timelapseName = "")
        {
            Phocalstream_Shared.Data.Model.Photo.User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            long id = CollectionService.NewTimelapseCollection(user, timelapseName, photoIds);
            return RedirectToAction("Timelapse", new { @collectionId = id });
        }

        [HttpPost]
        public ActionResult AddTag(long photoID, string tags)
        {
           Photo photo = PhotoService.AddTag(photoID, tags);

            if(photo == null)
            {
                return new HttpNotFoundResult(string.Format("Photo {0} was not found", photoID));
            }

            return PartialView("_TagPartial", photo);
        }

        [Authorize(Roles = @"Admin")]
        [HttpPost]
        public ActionResult DeleteTagFromPhoto(long photoID, long tagID)
        {
            Photo photo = PhotoService.DeleteTagFromPhoto(photoID, tagID);

            if (photo == null)
            {
                return new HttpNotFoundResult(string.Format("Photo {0} was not found", photoID));
            }

            return PartialView("_TagPartial", photo);
        }

        [HttpPost]
        public ActionResult TogglePhotoInUserCollection(long photoID, long collectionID)
        {
            CollectionService.TogglePhotoInUserCollection(photoID, collectionID);
            UserCollectionData model = LoadUserCollections(photoID);

            return PartialView("_UserCollectionPartial", model);
        }

        [HttpPost]
        public ActionResult NewUserCollection(string collectionName, long photoID)
        {
            User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            CollectionService.NewUserCollection(user, collectionName, Convert.ToString(photoID));

            UserCollectionData model = LoadUserCollections(photoID);

            return PartialView("_UserCollectionPartial", model);
        }

        public ActionResult DeleteDownload(string fileName)
        {
            if (fileName.Equals("ALL"))
            {
                FileInfo[] files = new DirectoryInfo(PathManager.GetDownloadPath()).GetFiles();
                foreach (var file in files)
                {
                    file.Delete();
                }
            }

            else
            {
                string filePath = PathManager.GetDownloadPath() + fileName;

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            return RedirectToAction("Downloads", "Home");
        }

        [Authorize(Roles = @"Admin")] 
        public ActionResult Tags()
        {
            var tags = TagRepository.GetAll(t => t.Photos).ToList();
            Tag tag = new Tag();
            return View(tags);
        }

        [Authorize(Roles = @"Admin")]
        public ActionResult DeleteTag(long tagID)
        {
            PhotoService.DeleteTag(tagID);
            return RedirectToAction("Tags");
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
            string downloadPath = PathManager.GetDownloadPath() + fileName;

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
    
        private CollectionViewModel GetCollectionViewModel(long siteID, long year)
        {
            CollectionViewModel model = new CollectionViewModel();

            model.Collection = CollectionRepository.First(c => c.Site.ID == siteID);
            model.SiteCoords = string.Format("{0}, {1}", model.Collection.Site.Latitude, model.Collection.Site.Longitude);

            List<Photo> photos = PhotoRepository.Find(p => p.Site.ID == model.Collection.Site.ID).OrderBy(p => p.Captured).ToList<Photo>();
            if (year != -1) 
            {
                photos = photos.Where(p => p.Captured.Year == year).ToList<Photo>();
            }

            model.SiteDetails = new SiteDetails() { PhotoCount = photos.Count(), First = photos.Select(p => p.Captured).First(), Last = photos.Select(p => p.Captured).Last() };

            Phocalstream_Shared.Data.Model.Photo.User User = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            if (User != null)
            {
                UserCollectionList userCollectionModel = new UserCollectionList();
                userCollectionModel.User = User;
                userCollectionModel.Collections = CollectionRepository.Find(c => c.Owner.ID == User.ID && c.Type == CollectionType.USER, c => c.Photos).ToList();
                model.UserCollections = userCollectionModel;
            }

            return model;
        }
    
        private List<ThumbnailModel> GetSiteYearSummary(long siteID)
        {
            List<ThumbnailModel> Years = new List<ThumbnailModel>();

            string siteName = CollectionRepository.Find(c => c.Site.ID == siteID).FirstOrDefault().Site.Name;

            List<int> yearStrings = PhotoRepository.Find(p => p.Site.ID == siteID).Select(p => p.Captured.Year).Distinct().ToList<int>();

            foreach (int y in yearStrings)
            {
                ThumbnailModel model = new ThumbnailModel();

                model.Name = Convert.ToString(y);

                Photo[] photos = PhotoRepository.Find(p => p.Site.ID == siteID && p.Captured.Year == y)
                                                            .ToArray();
                model.PhotoCount = photos.Count();
                model.First = photos[0].Captured;
                model.Last = photos[photos.Count() - 1].Captured;
                
                photos = photos.Where(p => p.Captured.Hour > 12 && p.Captured.Hour < 16).ToArray();

                Random rand = new Random();
                model.CoverPhotoID = photos[rand.Next(photos.Length)].ID;

                model.Link = "/Search/Index?site=" + siteName + "&year=" + model.Name;

                Years.Add(model);
            }

            return Years;
        }

        private PhotoFrequencyData GetPhotoFrequencyData(long siteID)
        {
            PhotoFrequencyData model = new PhotoFrequencyData();

            var data = PhotoRepository.Find(p => p.Site.ID == siteID)
                                      .GroupBy(p => p.Captured.Date)
                                      .Select(group => new
                                        {
                                            Date = group.Key,
                                            Count = group.Count()
                                        })
                                      .OrderBy(x => x.Date);

            model.FrequencyDataValues = "";
            foreach (var d in data)
            {
                model.FrequencyDataValues += d.Count + ", ";
            }

            model.FrequencyDataValues = model.FrequencyDataValues.Substring(0, model.FrequencyDataValues.Length - 2);

            return model;
        }

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
            data.DataTypes = WaterRepository.FetchBestDataTypesForStationDate(WaterRepository.GetClosestStations(siteLat, siteLong, 1), date).FirstOrDefault();
            data.chartDataValues = "";
            
            if (data.DataTypes != null)
            {
                data.WaterDataValues = WaterRepository.FetchByDateRange(data.DataTypes.StationID, data.DataTypes.DataID, date.AddDays(-42), date);
                data.ParameterInfo = WaterRepository.GetParameterCodeInfoFromDataType(data.DataTypes.DataID);
                data.ClosestStation = WaterRepository.GetStationInfo(data.DataTypes.StationID);

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

                // Only remove the ending comma if it was added
                if (data.chartDataValues.Length > 0)
                {
                    data.chartDataValues = data.chartDataValues.Substring(0, data.chartDataValues.Length - 2);
                }
            }
            else
            {
                data.WaterDataValues = new List<WaterDataValue>();
            }

            return data;
        } //End Load DM Data

        private DroughtMonitorWeek LoadDMDataValues(DMDataType type, DateTime date, int CountyFIPS)
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
                week = new DroughtMonitorWeek()
                {
                    D0 = 0,
                    D1 = 0,
                    D2 = 0,
                    D3 = 0,
                    D4 = 0,
                    NonDrought = 0,
                    Week = date,
                    Type = type,
                    County = county,
                    State = county.State
                };
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

        private UserCollectionData LoadUserCollections(long photoID)
        {
            Phocalstream_Shared.Data.Model.Photo.User User = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            if (User != null)
            {
                UserCollectionData model = new UserCollectionData();
                model.PhotoID = photoID;

                List<UserCollection> userCollections = new List<UserCollection>();
                IEnumerable<Collection> collections = CollectionRepository.Find(c => c.Owner.ID == User.ID && c.Site == null && c.Type != CollectionType.TIMELAPSE, c => c.Photos);

                foreach (var col in collections)
                {
                    UserCollection userCollection = new UserCollection();

                    userCollection.CollectionID = col.ID;
                    userCollection.CollectionName = col.Name;

                    userCollection.Added = col.Photos.Select(p => p.ID).Contains(photoID);

                    userCollections.Add(userCollection);
                }

                model.Collections = userCollections;
                return model;
            }
            else
            {
                return null;
            }
        }
    }
}
