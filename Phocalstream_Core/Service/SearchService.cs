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
        public IPhotoRepository PhotoRepo { get; set; }

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

        //Do these delete methods need to be secured?
        public void DeleteAllSearches()
        {
            IEnumerable<Collection> collections = CollectionRepository.Find(c => c.Type == CollectionType.SEARCH);

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

        public List<string> GetSiteNames()
        {
            //get the site ids that are actual camera sites
            List<long> ids = CollectionRepository.Find(c => c.Type == CollectionType.SITE, c => c.Site).Select(c => c.Site.ID).ToList<long>();

            //Get list of all site names to match to query
            List<string> siteNames = SiteRepository.Find(s => ids.Contains(s.ID)).Select(s => s.Name).ToList<string>();

            return siteNames;
        }

        public int SearchResultCount(SearchModel model)
        {
            string select = GetSearchQuery(model);
            List<long> ids = QuickSearch(select);
            return ids.Count;
        }

        public long SearchResultPhotoId(SearchModel model)
        {
            string select = GetSearchQuery(model);
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

        public List<long> SearchResultPhotoIds(SearchModel model)
        {
            string select = GetSearchQuery(model);
            List<long> ids = QuickSearch(select);

            return ids;
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

        public void ValidateCache(SearchModel model, int currentCount)
        {
            var collectionName = model.CreateCollectionName();
            var containerID = collectionName.GetHashCode().ToString();

            Collection collection = CollectionRepository.Find(c => c.ContainerID == containerID, c => c.Photos).FirstOrDefault();

            if (collection != null)
            {
                if (collection.Photos.Count() != currentCount)
                {
                    DeleteSearch(collection.ID);
                }
            }
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
                result.Matches = PhotoRepository.Find(p => result.Ids.Contains(p.ID), p => p.Site).ToList<Photo>();
            }
            
            return result;
        }

        private String GetSearchQuery(SearchModel model)
        {
            StringBuilder select = new StringBuilder();
            StringBuilder parameters = new StringBuilder();

            StringBuilder publicPhotosBuilder = new StringBuilder();

            StringBuilder sitesBuilder = new StringBuilder();
            StringBuilder tagBuilder = new StringBuilder();
            StringBuilder monthBuilder = new StringBuilder();
            StringBuilder dateBuilder = new StringBuilder();
            StringBuilder hourBuilder = new StringBuilder();

            publicPhotosBuilder = PublicPhotosQuery();


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

            parameters.Append("(" + publicPhotosBuilder + ")");

            if (sitesBuilder.Length != 0)
            {
                select.Append("INNER JOIN CameraSites ON Photos.Site_ID = CameraSites.ID ");
                parameters.Append(" AND " + "(" + sitesBuilder + ")");
            }

            if (monthBuilder.Length != 0)
            {
                parameters.Append(" AND " + "(" + monthBuilder + ")");
            }

            if (dateBuilder.Length != 0)
            {
                parameters.Append(" AND " + "(" + dateBuilder + ")");
            }

            if (hourBuilder.Length != 0)
            {
                parameters.Append(" AND " + "(" + hourBuilder + ")");
            }

            if (tagBuilder.Length != 0)
            {
                select.Append("INNER JOIN PhotoTags ON Photos.ID = PhotoTags.Photo_ID " +
                    "INNER JOIN Tags ON PhotoTags.Tag_ID = Tags.ID ");
                parameters.Append(" AND " + "(" + tagBuilder + ")");
            }

           select.Append("WHERE " + parameters);

           if (!String.IsNullOrWhiteSpace(model.Group))
           {
               select.Append(" ORDER BY");

               if (model.Group.Equals("site"))
               {
                   select.Append(" Photos.Site_ID,");
               }

               select.Append(" Photos.Captured");
           }
 
           return select.ToString();
        }

        private StringBuilder PublicPhotosQuery()
        {
            StringBuilder publicQuery = new StringBuilder();

            publicQuery.Append("Photos.Site_ID IN " +
                "(SELECT Site_ID from Collections WHERE Collections.Type = 0)");

            return publicQuery;
        }

        private StringBuilder SiteQuery(string query)
        {
            StringBuilder sitesBuilder = new StringBuilder();

            //Get list of all site names to match to query
            List<string> siteNames = GetSiteNames();
            string[] sites = query.Split(',');

            bool first = true;
            foreach (var site in sites)
            {
                if (siteNames.Contains(site))
                {
                    string new_site = site.Replace("'", "''");
                    if (first)
                    {
                        sitesBuilder.Append(string.Format("CameraSites.Name = '{0}' ", new_site));
                        first = false;
                    }
                    else
                    {
                        sitesBuilder.Append(string.Format("OR CameraSites.Name = '{0}' ", new_site));
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
                if (!tag.Equals("undefined"))
                {
                    if (tagNames.Contains(tag))
                    {
                        string new_tag = tag.Replace("'", "''");
                        if (tagFirst)
                        {
                            tagBuilder.Append(string.Format("Tags.Name = '{0}'", new_tag));
                            tagFirst = false;
                        }
                        else
                        {
                            tagBuilder.Append(string.Format("OR Tags.Name = '{0}'", new_tag));
                        }
                    }
                    else
                    {
                        if (tagFirst)
                        {
                            tagBuilder.Append(string.Format("Tags.Name = '{0}'", "null"));
                            tagFirst = false;
                        }
                        else
                        {
                            tagBuilder.Append(string.Format("OR Tags.Name = '{0}'", "null"));
                        }
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
                string tempDateString = "";

                //case: mm/dd/yyyy to mm/dd/yyyy
                if (d.Contains("to"))
                {
                    try
                    {
                        string[] dateRange = d.Split(new string[] { "to" }, StringSplitOptions.None);

                        //get the first date
                        DateTime first_date = DateTime.Parse(dateRange[0]);
                        DateTime second_date = DateTime.Parse(dateRange[1]);

                        tempDateString = string.Format("Photos.Captured BETWEEN '{0}' AND '{1}' ", first_date, second_date);
                    }
                    catch (FormatException e) { }
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
                        //catch the case where someone just enters a year
                        if (d.Length == 4)
                        {
                            try
                            {
                                DateTime new_first_date = DateTime.Parse("1/1/" + d);
                                DateTime new_second_date = DateTime.Parse("12/31/" + d).AddDays(1);

                                tempDateString = string.Format("Photos.Captured BETWEEN '{0}' AND '{1}' ", new_first_date, new_second_date);
                            }
                            catch (FormatException ex) { }
                        }
                    }
                    catch (InvalidDataException e) { }
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
