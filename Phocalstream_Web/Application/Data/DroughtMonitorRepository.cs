using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.External;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Application.Data
{
    public class DroughtMonitorRepository : IDroughtMonitorRepository
    {
        private string _connectionString;

        public DroughtMonitorRepository(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("The DM data repository requires an SQL connection");
            }
            _connectionString = connectionString;
        }

        public ICollection<DroughtMonitorWeek> Fetch(DMDataType type)
        {
            if (type == DMDataType.ALL)
            {
                List<DroughtMonitorWeek> weeks = new List<DroughtMonitorWeek>();
                weeks.AddRange(GetValuesFor(DMDataType.COUNTY));
                weeks.AddRange(GetValuesFor(DMDataType.STATE));
                weeks.AddRange(GetValuesFor(DMDataType.US));

                return weeks;
            }
            else
            {
                return GetValuesFor(type);
            }
        }

        public void Add(DroughtMonitorWeek week)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(null, conn))
                {
                    switch (week.Type)
                    {
                        case DMDataType.COUNTY:
                            command.CommandText = "insert into CountyDMValues (PublishedDate, County_ID, DroughtCategory, DroughtValue) values (@pdate, @county, @category, @dval)";
                            command.Parameters.AddWithValue("@county", week.County.ID);
                            break;
                        case DMDataType.STATE:
                            command.CommandText = "insert into StateDMValues (PublishedDate, State_ID, DroughtCategory, DroughtValue) values (@pdate, @state, @category, @dval)";
                            command.Parameters.AddWithValue("@state", week.State.ID);
                            break;
                        case DMDataType.US:
                            command.CommandText = "insert into USDMValues (PublishedDate, DroughtCategory, DroughtValue) values (@pdate, @category, @dval)";
                            break;
                    }
                    command.Parameters.AddWithValue("@pdate", week.Week);
                    command.Parameters.Add("@category", SqlDbType.Int);
                    command.Parameters.Add("@dval", SqlDbType.Float);

                    command.Parameters["@category"].Value = 0;
                    command.Parameters["@dval"].Value = week.NonDrought;
                    command.ExecuteNonQuery();

                    command.Parameters["@category"].Value = 1;
                    command.Parameters["@dval"].Value = week.D0;
                    command.ExecuteNonQuery();

                    command.Parameters["@category"].Value = 2;
                    command.Parameters["@dval"].Value = week.D1;
                    command.ExecuteNonQuery();

                    command.Parameters["@category"].Value = 3;
                    command.Parameters["@dval"].Value = week.D2;
                    command.ExecuteNonQuery();

                    command.Parameters["@category"].Value = 4;
                    command.Parameters["@dval"].Value = week.D3;
                    command.ExecuteNonQuery();
                    
                    command.Parameters["@category"].Value = 5;
                    command.Parameters["@dval"].Value = week.D4;
                    command.ExecuteNonQuery();
                }
            }

            throw new NotImplementedException();
        }


        public ICollection<DroughtMonitorWeek> FindBy(USCounty county, DateTime? week = null, int weeksPrevious = 0)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(null, conn))
                {
                    if (week == null && weeksPrevious == 0)
                    {
                        command.CommandText = string.Format("select DroughtCategory, DroughtValue, PublishedDate, County_ID from CountyDMValues where County_ID = @county order by PublishedDate, County_ID");
                        command.Parameters.AddWithValue("@county", county.ID);
                    }
                    else if (week == null && weeksPrevious != null)
                    {
                        command.CommandText = string.Format("select DroughtCategory, DroughtValue, PublishedDate, County_ID from CountyDMValues where County_ID = @county and PublishedDate >= @rangestart order by PublishedDate, County_ID");
                        command.Parameters.AddWithValue("@county", county.ID);
                        command.Parameters.AddWithValue("@rangestart", DateTime.Now.AddDays(7 * (0 - weeksPrevious)));
                    }
                    else if (week != null)
                    {
                        command.CommandText = string.Format("select DroughtCategory, DroughtValue, PublishedDate, County_ID from CountyDMValues where County_ID = @county and PublishedDate >= @rangestart and PublishedDate <= @rangeend order by PublishedDate, County_ID");
                        command.Parameters.AddWithValue("@county", county.ID);
                        command.Parameters.AddWithValue("@rangestart", week.Value.AddDays(7 * (0 - weeksPrevious)));
                        command.Parameters.AddWithValue("@rangeend", week);
                    }

                    return ProcessQuery(command, DMDataType.COUNTY);
                }
            }
        }

        public ICollection<DroughtMonitorWeek> FindBy(USState state, DateTime? week = null, int weeksPrevious = 0)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(null, conn))
                {
                    if (week == null && weeksPrevious == 0)
                    {
                        command.CommandText = string.Format("select DroughtCategory, DroughtValue, PublishedDate, State_ID from StateDMValues where State_ID = @state order by PublishedDate, County_ID");
                        command.Parameters.AddWithValue("@state", state.ID);
                    }
                    else if (week == null && weeksPrevious != null)
                    {
                        command.CommandText = string.Format("select DroughtCategory, DroughtValue, PublishedDate, State_ID from StateDMValues where State_ID = @state and PublishedDate >= @rangestart order by PublishedDate, County_ID");
                        command.Parameters.AddWithValue("@state", state.ID);
                        command.Parameters.AddWithValue("@rangestart", DateTime.Now.AddDays(7 * (0 - weeksPrevious)));
                    }
                    else if (week != null)
                    {
                        command.CommandText = string.Format("select DroughtCategory, DroughtValue, PublishedDate, State_ID from StateDMValues where State_ID = @state and PublishedDate >= @rangestart and PublishedDate <= @rangeend order by PublishedDate, County_ID");
                        command.Parameters.AddWithValue("@state", state.ID);
                        command.Parameters.AddWithValue("@rangestart", week.Value.AddDays(7 * (0 - weeksPrevious)));
                        command.Parameters.AddWithValue("@rangeend", week);
                    }

                    return ProcessQuery(command, DMDataType.STATE);
                }
            }
        }

        public ICollection<DroughtMonitorWeek> FindUS(DateTime? week = null, int weeksPrevious = 0)
        {
            throw new NotImplementedException();
        }

        public ICollection<DroughtMonitorWeek> Find(DateTime? week = null, int weeksPrevious = 0)
        {
            throw new NotImplementedException();
        }

        private List<DroughtMonitorWeek> GetValuesFor(DMDataType type)
        {
            List<DroughtMonitorWeek> weeks = new List<DroughtMonitorWeek>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(null, conn))
                {
                    switch (type)
                    {
                        case DMDataType.COUNTY:
                            command.CommandText = string.Format("select DroughtCategory, DroughtValue, PublishedDate, County_ID from CountyDMValues order by PublishedDate, County_ID");
                            break;
                        case DMDataType.STATE:
                            command.CommandText = string.Format("select DroughtCategory, DroughtValue, PublishedDate, State_ID from StateDMValues order by PublishedDate, State_ID");
                            break;
                        case DMDataType.US:
                            command.CommandText = string.Format("select DroughtCategory, DroughtValue, PublishedDate from USDMValues order by PublishedDate");
                            break;
                    }
                    weeks.AddRange(ProcessQuery(command, type));
                }
            }

            return weeks;
        }

        private ICollection<DroughtMonitorWeek> ProcessQuery(SqlCommand command, DMDataType type)
        {
            List<DroughtMonitorWeek> weeks = new List<DroughtMonitorWeek>();
            using (SqlDataReader reader = command.ExecuteReader())
            {
                DroughtMonitorWeek currentWeek = null;
                while (reader.Read())
                {
                    if (currentWeek == null)
                    {
                        currentWeek = new DroughtMonitorWeek();
                    }
                    else
                    {
                        switch (type)
                        {
                            case DMDataType.COUNTY:
                                if (reader.GetDateTime(2) != currentWeek.Week || reader.GetInt64(3) != currentWeek.County.ID)
                                {
                                    weeks.Add(currentWeek);
                                    currentWeek = new DroughtMonitorWeek();
                                }
                                break;
                            case DMDataType.STATE:
                                if (reader.GetDateTime(2) != currentWeek.Week || reader.GetInt64(3) != currentWeek.State.ID)
                                {
                                    weeks.Add(currentWeek);
                                    currentWeek = new DroughtMonitorWeek();
                                }
                                break;
                            case DMDataType.US:
                                if (reader.GetDateTime(2) != currentWeek.Week)
                                {
                                    weeks.Add(currentWeek);
                                    currentWeek = new DroughtMonitorWeek();
                                }
                                break;
                        }
                    }

                    currentWeek[reader.GetInt32(0)] = reader.GetFloat(1);
                }
                reader.Close();
            }
            return weeks;
        }
    }
}