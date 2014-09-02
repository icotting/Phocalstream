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
            if (!Directory.Exists(search_path))
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

        public int SearchResultCount(QuickSearchModel model)
        {
            string select = GetQuickSearchQuery(model);
            List<long> ids = QuickSearch(select);
            return ids.Count;
        }

        public long SearchResultPhotoId(QuickSearchModel model)
        {
            string select = GetQuickSearchQuery(model);
            List<long> ids = QuickSearch(select);

            if (ids.Count > 0)
            {
                Random r = new Random();
                int index = r.Next(ids.Count);
                return ids[index];
            }
            else
            {
                return 0;
            }
        }

        public List<long> QuickSearch(string query)
        {
            List<long> Ids = new List<long>();

            if (!String.IsNullOrWhiteSpace(query))
            {
                //run the query (only if there are parameters selected)
                using (SqlConnection conn = new SqlConnection(PathManager.GetDbConnection()))
                {
                    conn.Open();

                    using (SqlCommand command = new SqlCommand(query.ToString(), conn))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Ids.Add((long)reader["ID"]);
                            }
                        }
                    }
                }
            }

            return Ids;
        }

        public SearchMatches Search(SearchModel model)
        {
            SearchMatches result = new SearchMatches();
            result.Ids = new List<long>();
            
            string select = GetSearchQuery(model);

            if (!String.IsNullOrWhiteSpace(select)) 
            {
                //run the query (only if there are parameters selected)
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
            }
            
            return result;
        }

        private string GetQuickSearchQuery(QuickSearchModel model)
        {
            StringBuilder select = new StringBuilder();
            StringBuilder parameters = new StringBuilder();

            StringBuilder sitesBuilder = new StringBuilder();
            StringBuilder tagBuilder = new StringBuilder();
            StringBuilder monthBuilder = new StringBuilder();
            StringBuilder dateBuilder = new StringBuilder();
            StringBuilder hourBuilder = new StringBuilder();

            //Sites
            if (!String.IsNullOrWhiteSpace(model.Sites))
            {
                sitesBuilder = SiteQuery(model.Sites);
            }

            //tag
            if (!String.IsNullOrWhiteSpace(model.Tags))
            {
                tagBuilder = TagQuery(model.Tags);
            }

            //date
            if (!String.IsNullOrWhiteSpace(model.Dates))
            {
                dateBuilder = DateQuery(model.Dates);
            }

            //months
            if (!String.IsNullOrWhiteSpace(model.Months))
            {
                monthBuilder.Append(string.Format("MONTH(Photos.Captured) IN ({0}) ", model.Months));
            }

            //hours
            if (!String.IsNullOrWhiteSpace(model.Hours))
            {
                hourBuilder.Append(string.Format("DATEPART(hh, Photos.Captured) IN ({0}) ", model.Hours));
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
        
            return select.ToString();
        }

        private String GetSearchQuery(SearchModel model)
        {
            StringBuilder select = new StringBuilder();
            StringBuilder parameters = new StringBuilder();

            StringBuilder sitesBuilder = new StringBuilder();
            StringBuilder tagBuilder = new StringBuilder();
            StringBuilder monthBuilder = new StringBuilder();
            StringBuilder dateBuilder = new StringBuilder();
            StringBuilder hourBuilder = new StringBuilder();

            //Sites
            if (!String.IsNullOrWhiteSpace(model.Sites))
            {
                sitesBuilder = SiteQuery(model.Sites);
            }

            //tag
            if (!String.IsNullOrWhiteSpace(model.Tags))
            {
                tagBuilder = TagQuery(model.Tags);
            }

            //date
            if (!String.IsNullOrWhiteSpace(model.Dates))
            {
                dateBuilder = DateQuery(model.Dates);
            }

            //months
            if (!String.IsNullOrWhiteSpace(model.Months))
            {
                monthBuilder.Append(string.Format("MONTH(Photos.Captured) IN ({0}) ", model.Months));
            }

            //hours
            if (!String.IsNullOrWhiteSpace(model.Hours))
            {
                hourBuilder.Append(string.Format("DATEPART(hh, Photos.Captured) IN ({0}) ", model.Hours));
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

                return select.ToString();
            }
            else 
            {
                return "";
            }
        }

        private StringBuilder SiteQuery(string query)
        {
            StringBuilder sitesBuilder = new StringBuilder();
            //Get list of all site names to match to query
            List<string> siteNames = SiteRepository.GetAll().Select(s => s.Name).ToList<string>();
            string[] sites = query.Split(',');

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

            return sitesBuilder;
        }
    
        private StringBuilder TagQuery(string query)
        {
            StringBuilder tagBuilder = new StringBuilder();

            List<string> tagNames = TagRepository.GetAll().Select(t => t.Name).ToList<string>();
            string[] tags = query.Split(',');

            bool tagFirst = true;
            foreach (var tag in tags)
            {
                if (tagNames.Contains(tag))
                {
                    if(tagFirst)
                    {
                        tagBuilder.Append(string.Format("Tags.Name = '{0}'", tag));
                        tagFirst = false;
                    }
                    else
                    {
                        tagBuilder.Append(string.Format("OR Tags.Name = '{0}'", tag));
                    }
                }
            }

            return tagBuilder;
        }
    
        private StringBuilder DateQuery(string query)
        {
            StringBuilder dateBuilder = new StringBuilder();

            string[] dates = query.Split(',');

            bool firstDate = true;
            foreach (var d in dates)
            {
                string tempDateString;

                //case: mm/dd/yyyy to mm/dd/yyyy
                if (d.Contains("to"))
                {
                    string[] dateRange = d.Split(new string[] { "to" }, StringSplitOptions.None);

                    //get the first date
                    DateTime first_date = DateTime.Parse(dateRange[0]);
                    DateTime second_date = DateTime.Parse(dateRange[1]);

                    tempDateString = string.Format("Photos.Captured BETWEEN '{0}' AND '{1}' ", first_date, second_date);
                }
                else
                {
                    //try to parse full date
                    try
                    {
                        DateTime date = DateTime.Parse(d);
                        tempDateString = string.Format("MONTH(Photos.Captured) = {0} AND DAY(Photos.Captured) = {1} AND YEAR(Photos.Captured) = {2} ",
                                                date.Month, date.Day, date.Year);
                    }
                    catch (FormatException e)
                    {
                        tempDateString = "";
                    }
                    catch (InvalidDataException e)
                    {
                        tempDateString = "";
                    }

                }

                if (!String.IsNullOrWhiteSpace(tempDateString))
                {
                    if (!firstDate)
                    {
                        dateBuilder.Append("OR ");
                    }
                    dateBuilder.Append(tempDateString);

                    firstDate = false;
                }
            }

            return dateBuilder;
        }
    }
}
