using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Data.Model.View;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Application.Data
{
    public class PhotoRepository : IPhotoRepository
    {
        public SiteDetails GetSiteDetails(CameraSite site)
        {
            SiteDetails details = new SiteDetails();
            details.SiteName = site.Name;
            details.SiteID = site.ID;

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand("select min(Captured), max(Captured), count(*), max(ID) from Photos where Site_ID = @siteID", conn))
                {
                    command.Parameters.AddWithValue("@siteID", site.ID);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            details.First = reader.GetDateTime(0);
                            details.Last = reader.GetDateTime(1);
                            details.PhotoCount = reader.GetInt32(2);
                            details.LastPhotoID = reader.GetInt64(3);
                        }
                    }
                }
            }
            return details;
        }
    }
}