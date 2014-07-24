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
using Phocalstream_Shared.Data.Model.View;
using System.Data.SqlClient;

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

        public SearchMatches Search(SearchModel model)
        {
            SearchMatches result = new SearchMatches();

            string hourString = model.CreateHourString();
            string monthString = model.CreateMonthString();

            StringBuilder select = new StringBuilder();
            StringBuilder parameters = new StringBuilder();
            
            StringBuilder sitesBuilder = new StringBuilder();
            StringBuilder tagBuilder = new StringBuilder();
            StringBuilder monthBuilder = new StringBuilder();
            StringBuilder dateBuilder = new StringBuilder();
            StringBuilder hourBuilder = new StringBuilder();
            
            //Sites
            if(!String.IsNullOrWhiteSpace(model.Sites))
            {
                List<string> siteNames = SiteRepository.GetAll().Select(s => s.Name).ToList<string>();
                string[] sites = model.Sites.Split(',');

                bool first = true;
                foreach (var site in sites)
                {
                    if (siteNames.Contains(site))
                    {
                        if (first)
                        {
                            sitesBuilder.Append(string.Format("CameraSites.Name = '{0}' ", site));
                            first = false;
                        }
                        else
                        {
                            sitesBuilder.Append(string.Format("OR CameraSites.Name = '{0}' ", site));
                        }
                    }
                }
            }

            //tag
            if (!String.IsNullOrWhiteSpace(model.Tags))
            {
                List<string> tagNames = TagRepository.GetAll().Select(t => t.Name).ToList<string>();
                string[] tags = model.Tags.Split(',');

                bool tagFirst = true;
                foreach (var tag in tags)
                {
                    if (tagNames.Contains(tag))
                    {
                        if (!tagFirst)
                        {
                            tagBuilder.Append("OR ");
                        }
                        tagBuilder.Append(string.Format("Tags.Name = '{0}'", tag));

                        tagFirst = false;
                    }
                }
            }

            //date
            if (!String.IsNullOrWhiteSpace(model.Dates))
            {
                string[] dates = model.Dates.Split(',');

                bool firstDate = true;
                foreach (var d in dates)
                {
                    DateTime date = DateTime.Parse(d);
                    if (!firstDate)
                    {
                        dateBuilder.Append("OR ");
                    }
                    dateBuilder.Append(string.Format("MONTH(Photos.Captured) = {0} AND DAY(Photos.Captured) = {1} AND YEAR(Photos.Captured) = {2} ",
                        date.Month, date.Day, date.Year));
                    
                    firstDate = false;
                }
            }

            //months
            if (!String.IsNullOrWhiteSpace(monthString))
            {
                monthBuilder.Append(string.Format("MONTH(Photos.Captured) IN ({0}) ", monthString));
            }

            //hours
            if (!String.IsNullOrWhiteSpace(hourString))
            {
                hourBuilder.Append(string.Format("DATEPART(hh, Photos.Captured) IN ({0}) ", hourString));
            }


            //merge the builders
            select.Append("select Photos.ID from Photos ");

            if (sitesBuilder.Length != 0)
            {
                select.Append("INNER JOIN CameraSites ON Photos.Site_ID = CameraSites.ID ");
                parameters.Append(sitesBuilder + "AND ");
            }

            if (monthBuilder.Length != 0)
            {
                parameters.Append(monthBuilder + "AND ");
            }

            if (dateBuilder.Length != 0)
            {
                parameters.Append(dateBuilder + "AND ");
            }

            if (hourBuilder.Length != 0)
            {
                parameters.Append(hourBuilder + "AND ");
            }

            if (tagBuilder.Length != 0)
            {
                select.Append("INNER JOIN PhotoTags ON Photos.ID = PhotoTags.Photo_ID " +
                    "INNER JOIN Tags ON PhotoTags.Tag_ID = Tags.ID ");
                parameters.Append(tagBuilder);
            }

            //remove final AND if present
            if (parameters.Length > 0)
            {
                if (parameters.ToString().EndsWith("AND "))
                {
                    parameters.Remove(parameters.Length - 4, 4);
                }
                select.Append("WHERE " + parameters);
            }

            //run the query
            result.Ids = new List<long>();
            using (SqlConnection conn = new SqlConnection(PathManager.GetDbConnection()))
            {
                conn.Open();

                using (SqlCommand command = new SqlCommand(select.ToString(), conn))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Ids.Add((long)reader["ID"]);
                        }
                    }
                }
            }
            result.Matches = PhotoRepository.Find(p => result.Ids.Contains(p.ID)).ToList<Photo>();

            return result;
        }
    }
}
