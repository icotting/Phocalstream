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

        public List<string> GetSiteNames()
        {
            //get the site ids that are actual camera sites
            List<long> ids = CollectionRepository.Find(c => c.Type == CollectionType.SITE, c => c.Site).Select(c => c.Site.ID).ToList<long>();

            //Get list of all site names to match to query
            List<string> siteNames = SiteRepository.Find(s => ids.Contains(s.ID)).Select(s => s.Name).ToList<string>();

            return siteNames;
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

            return Ids.Distinct().ToList<long>();
        }

        private String GetSearchQuery(SearchModel model)
        {
            StringBuilder select = new StringBuilder();
            StringBuilder parameters = new StringBuilder();

            StringBuilder publicPhotosBuilder = new StringBuilder();

            StringBuilder collectionBuilder = new StringBuilder();
            StringBuilder sitesBuilder = new StringBuilder();
            StringBuilder tagBuilder = new StringBuilder();
            StringBuilder monthBuilder = new StringBuilder();
            StringBuilder dateBuilder = new StringBuilder();
            StringBuilder hourBuilder = new StringBuilder();

            publicPhotosBuilder = PublicPhotosQuery(model.UserId, model.CameraSites, model.PublicUserCollections);

            if (!String.IsNullOrWhiteSpace(model.CollectionId))
            {
                collectionBuilder = CollectionQuery(model.CollectionId);
            }

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
            select.Append("SELECT Photos.ID from Photos ");
            select.Append("LEFT JOIN CollectionPhotos ON Photos.ID = CollectionPhotos.PhotoId ");
            
            parameters.Append("(" + publicPhotosBuilder + ")");

            if (collectionBuilder.Length != 0)
            {
                parameters.Append(" AND " + "(" + collectionBuilder + ")");
            }

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

        private StringBuilder PublicPhotosQuery(string userId, bool cameraSites, bool publicUserCollections)
        {
            StringBuilder publicQuery = new StringBuilder();

            // Initial query to capture photos from sites
            if (cameraSites) 
            {
                publicQuery.Append("Photos.Site_ID IN (SELECT Site_ID from Collections WHERE Collections.Type = 0) ");
            }
            else
            {
                publicQuery.Append("Photos.Site_ID NOT IN (SELECT Site_ID from Collections WHERE Collections.Type = 0) ");
            }

            if (publicUserCollections)
            {
                publicQuery.Append("OR ");
                publicQuery.Append("CollectionPhotos.CollectionId IN (SELECT ID from Collections WHERE Collections.[Public] = 1) ");

                // If a user is signed in, capture their private collections
                if (!String.IsNullOrWhiteSpace(userId))
                {
                    publicQuery.Append("OR ");
                    publicQuery.Append(string.Format("CollectionPhotos.CollectionId IN (SELECT ID from Collections WHERE Collections.Type = 1 AND Collections.Owner_ID = {0}) ", userId));
                }
            }

            return publicQuery;
        }

        private StringBuilder CollectionQuery(string collectionId)
        {
            StringBuilder collectionBuilder = new StringBuilder();

            try
            {
                var id = long.Parse(collectionId);
                collectionBuilder.Append(string.Format("CollectionPhotos.CollectionId = {0}", id));
            }
            catch (FormatException ex)
            {

            }

            return collectionBuilder;
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
