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
                            if (week.County.ID == 0)
                            {
                                week.County.ID = this.GetCountyID(conn, week.County.Name, week.County.Fips, week.State.Name);
                            }
                            command.CommandText = "insert into CountyDMValues (PublishedDate, County_ID, DroughtCategory, DroughtValue) values (@pdate, @county, @category, @dval)";
                            command.Parameters.AddWithValue("@county", week.County.ID);
                            break;
                        case DMDataType.STATE:
                            if (week.State.ID == 0)
                            {
                                week.State.ID = this.GetStateID(conn, week.State.Name);
                            }
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

                    for (int i = 0; i < 6; i++)
                    {
                        command.Parameters["@category"].Value = i;
                        command.Parameters["@dval"].Value = week[i];
                        command.ExecuteNonQuery();
                    }

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
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(null, conn))
                {
                    if (week == null && weeksPrevious == 0)
                    {
                        command.CommandText = string.Format("select DroughtCategory, DroughtValue, PublishedDate from USDMValues order by PublishedDate");
                    }
                    else if (week == null && weeksPrevious != null)
                    {
                        command.CommandText = string.Format("select DroughtCategory, DroughtValue, PublishedDate from USDMValues where PublishedDate >= @rangestart order by PublishedDate");
                        command.Parameters.AddWithValue("@rangestart", DateTime.Now.AddDays(7 * (0 - weeksPrevious)));
                    }
                    else if (week != null)
                    {
                        command.CommandText = string.Format("select DroughtCategory, DroughtValue, PublishedDate from USDMValues where PublishedDate >= @rangestart and PublishedDate <= @rangeend order by PublishedDate");
                        command.Parameters.AddWithValue("@rangestart", week.Value.AddDays(7 * (0 - weeksPrevious)));
                        command.Parameters.AddWithValue("@rangeend", week);
                    }

                    return ProcessQuery(command, DMDataType.US);
                }
            }
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

        private long GetCountyID(SqlConnection conn, string county, int FIPS, string state)
        {
            long countyID = -1;
            using (SqlCommand countyLookup = new SqlCommand(string.Format("select ID from Counties where FIPS={0}", FIPS), conn))
            using (SqlDataReader reader = countyLookup.ExecuteReader())
            {
                if (reader.Read())
                {
                    countyID = reader.GetInt64(0);
                }
                reader.Close();
                if (countyID == -1)
                {
                    // if county does not exist, add it
                    countyID = this.AddCounty(conn, county, FIPS, state);
                }
            }
            return countyID;
        } //End GetCountyID

        private long AddCounty(SqlConnection conn, string county, int FIPS, string state)
        {
            SqlCommand command = new SqlCommand(null, conn);
            // Create and prepare an SQL statement.
            command.CommandText = "insert into Counties (FIPS, Name, State_ID) values (@FIPS, @name, @state)";
            command.Parameters.AddWithValue("@FIPS", FIPS);
            command.Parameters.AddWithValue("@name", county);
            command.Parameters.AddWithValue("@state", this.GetStateID(conn, state));
            command.ExecuteNonQuery();

            // Read the new ID back to return
            command.Parameters.Clear();
            command.CommandText = "SELECT @@IDENTITY";
            return Convert.ToInt64(command.ExecuteScalar());
        } //End AddCounty

        private long GetStateID(SqlConnection conn, string state)
        {
            long stateID = -1;
            // Get State ID
            using (SqlCommand stateLookup = new SqlCommand(string.Format("select ID from States where Name='{0}'", state), conn))
            using (SqlDataReader stateReader = stateLookup.ExecuteReader())
            {
                if (stateReader.Read())
                {
                    stateID = stateReader.GetInt64(0);
                }
                stateReader.Close();
                if (stateID == -1)
                {
                    // if state does not exist for county, add it
                    stateID = this.AddState(conn, state);
                }
            }
            return stateID;
        } //End GetStateID

        private long AddState(SqlConnection conn, string state)
        {
            SqlCommand command = new SqlCommand(null, conn);
            // Create and prepare an SQL statement.
            command.CommandText = "insert into States (Name) values (@name)";
            command.Parameters.AddWithValue("@name", state);
            command.ExecuteNonQuery();

            // Read the new ID back to return
            command.Parameters.Clear();
            command.CommandText = "SELECT @@IDENTITY";
            return Convert.ToInt64(command.ExecuteScalar());
        } //End AddState


    }
}