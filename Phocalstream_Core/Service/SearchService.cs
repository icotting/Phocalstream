using Microsoft.DeepZoomTools;
using Microsoft.Practices.Unity;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace Phocalstream_Service.Service
{
    public class SearchService : ISearchService
    {
        [Dependency]
        public IEntityRepository<Photo> PhotoRepository { get; set; }

        [Dependency]
        public IEntityRepository<Tag> TagRepository { get; set; }

        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IEntityRepository<CameraSite> SiteRepository { get; set; }

        [Dependency]
        public IUnitOfWork Unit { get; set; }

        private const string MorningHours = "5,6,7,8,9,10,11";
        private const string AfternoonHours = "12,13,14,15,16";
        private const string EveningHours = "17,18,19,20";
        private const string NightHours = "21,22,23,0,1,2,3,4";

        private const string SpringMonths = "3,4,5";
        private const string SummerMonths = "6,7,8";
        private const string FallMonths = "9,10,11";
        private const string WinterMonths = "12,1,2";
        
        public string ValidateAndGetSearchPath()
        {
            string search_path = PathManager.GetSearchPath();
            if(!Directory.Exists(search_path))
            {
                Directory.CreateDirectory(search_path);
            }

            return search_path;
        }

        public void DeleteSearch(long collectionID)
        {
            Collection col = CollectionRepository.First(c => c.ID == collectionID && c.Type == CollectionType.SEARCH);

            if (col != null)
            {
                string filePath = Path.Combine(PathManager.GetSearchPath(), col.ContainerID);

                if (System.IO.Directory.Exists(filePath))
                {
                    System.IO.Directory.Delete(filePath, true);
                }

                CollectionRepository.Delete(col);
                Unit.Commit();
            }
        }

        public void DeleteAllSearches()
        {
            List<Collection> collections = CollectionRepository.Find(c => c.Type == CollectionType.SEARCH).ToList();

            foreach (var col in collections)
            {
                string filePath = Path.Combine(PathManager.GetSearchPath(), col.ContainerID);

                if (System.IO.Directory.Exists(filePath))
                {
                    System.IO.Directory.Delete(filePath, true);
                }

                CollectionRepository.Delete(col);
                Unit.Commit();
            }
        }

        public void GenerateCollectionManifest(List<string> fileNames, string savePath)
        {
            CollectionCreator creator = new CollectionCreator();
            creator.TileFormat = Microsoft.DeepZoomTools.ImageFormat.Jpg;
            creator.TileOverlap = 1;
            creator.TileSize = 256;

            creator.Create(fileNames, savePath);
        }

        public List<string> GetSiteNames()
        {
            return SiteRepository.GetAll().Select(s => s.Name).ToList<string>();
        }

        public List<Photo> GetPhotosBySite(string siteString)
        {
            List<Photo> matches = new List<Photo>();

            if (siteString != null)
            {
                string[] sites = siteString.Split(',');

                foreach (var site in sites)
                {
                    CameraSite cameraSite = SiteRepository.First(s => s.Name.Equals(site));
                    matches.AddRange(PhotoRepository.Find(p => p.Site.ID == cameraSite.ID));
                }
            }

            return matches;
        }

        public List<Photo> GetPhotosBySeason(string seasonString)
        {
            List<Photo> matches = new List<Photo>();

            if (seasonString != null)
            {
                string[] seasons = seasonString.Split(',');

                foreach (var season in seasons)
                {
                    switch(season)
                    {
                        case "Spring":
                            matches.AddRange(GetPhotosByMonth(SpringMonths));
                            break;
                        case "Summer":
                            matches.AddRange(GetPhotosByMonth(SummerMonths));
                            break;
                        case "Fall":
                            matches.AddRange(GetPhotosByMonth(FallMonths));
                            break;
                        case "Winter":
                            matches.AddRange(GetPhotosByMonth(WinterMonths));
                            break;
                        default:
                            break;
                    }
                }
            }

            return matches;
        }

        public List<Photo> GetPhotosByMonth(string monthString)
        {
            List<Photo> matches = new List<Photo>();

            if (monthString != null)
            {
                string[] months = monthString.Split(',');

                foreach (var m in months)
                {
                    int month = Convert.ToInt16(m);
                    matches.AddRange(PhotoRepository.Find(p => p.Captured.Month == month));
                }
            }

            return matches;
        }

        public List<Photo> GetPhotosByDate(string dateString)
        {
            List<Photo> matches = new List<Photo>();

            if (dateString != null)
            {
                string[] dates = dateString.Split(',');

                foreach (var d in dates)
                {
                    DateTime date = DateTime.Parse(d);
                    matches.AddRange(PhotoRepository.Find(p => p.Captured.Month == date.Month &&
                            p.Captured.Day == date.Day &&
                            p.Captured.Year == date.Year));
                }
            }

            return matches;
        }

        public List<Photo> GetPhotosByTag(string tagString)
        {
            List<Photo> matches = new List<Photo>();

            if (tagString != null)
            {
                string[] tags = tagString.Split(',');

                IEnumerable<Photo> photos = PhotoRepository.GetAll(p => p.Tags);
                foreach (var tagName in tags)
                {
                    Tag tag = TagRepository.First(t => t.Name.Equals(tagName));
                    matches.AddRange(photos.Where(p => p.Tags.Contains(tag)));
                }
            }

            return matches;
        }

        public List<Photo> GetPhotosByTimeOfDay(string timeString)
        {
            List<Photo> matches = new List<Photo>();

            if (timeString != null)
            {
                string[] times = timeString.Split(',');

                foreach (var time in times)
                {
                    switch (time)
                    {
                        case "Morning":
                            matches.AddRange(GetPhotosByHourOfDay(MorningHours));
                            break;
                        case "Afternoon":
                            matches.AddRange(GetPhotosByHourOfDay(AfternoonHours));
                            break;
                        case "Evening":
                            matches.AddRange(GetPhotosByHourOfDay(EveningHours));
                            break;
                        case "Night":
                            matches.AddRange(GetPhotosByHourOfDay(NightHours));
                            break;
                        default:
                            break;
                    }
                }
            }

            return matches;
        }

        public List<Photo> GetPhotosByHourOfDay(string hourString)
        {
            List<Photo> matches = new List<Photo>();

            if (hourString != null)
            {
                string[] times = hourString.Split(',');

                foreach (var t in times)
                {
                    int time = Convert.ToInt16(t);
                    matches.AddRange(PhotoRepository.Find(p => p.Captured.Hour == time));
                }
            }

            return matches;
        }
    }
}
