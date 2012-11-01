using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;

namespace Phocalstream_Web.Application.Data
{
                
    public enum DMDataType
    {
        COUNTY,
        STATE,
        US
    }

    public class DroughtMonitorImporter
    {
        private static DroughtMonitorImporter _instance;

        private bool _importRunning;
        private String _firstDate;
        private String _lastDate;

        public bool ImportRunning
        {
            get { return _importRunning; }
        }
        public String FirstDate
        {
            get { return _firstDate; }
        }
        public String LastDate
        {
            get { return _lastDate; }
        }

        private DroughtMonitorImporter()
        {
            this._firstDate = "None";
            this._lastDate = "None";
            this._importRunning = false;
            this.SetDates();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static DroughtMonitorImporter getInstance()
        {
            if (_instance == null)
            {
                _instance = new DroughtMonitorImporter();
            }
            return _instance;
        } //End getInstance

        public void RunDMImport(string type)
        {
            switch (type)
            {
                case "full":
                    // Import All DM data
                    this.RunDMImportAll();
                    break;
                case "current":
                    // Import Current week
                    this.RunDMImport(DateTime.Now);
                    break;
                default:
                    this.RunDMImport(DateTime.Now);
                    break;
            }
        } //End RunDMImport (type)

        public void RunDMImportAll()
        {
            DateTime startDate = new DateTime(2009, 1, 6);
            DateTime endDate = DateTime.UtcNow;
            this.RunDMImport(startDate, endDate);
        } //End RunDMImportAll

        public void RunDMImport(DateTime date)
        {
            this.RunDMImport(date, date);
        } //End RunDMImport (one date)

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RunDMImport(DateTime start, DateTime end)
        {
            if (!ImportRunning)
            {
                new Task(() =>
                {
                    this._importRunning = true;
                    this.ImportDMData(start, end);
                    this.SetDates(); //Reset the first and last dates to the new dates in the DB
                    this._importRunning = false;
                }).Start();
            }
        } //End RunDMImport (two dates)

        private void ImportDMData(DateTime startDate, DateTime endDate)
        {
            startDate = this.ConvertDateToTuesday(startDate);
            endDate = this.ConvertDateToTuesday(endDate);

            DateTime importWeek = startDate;
            List<DateTime> importDates = new List<DateTime>();
            while (importWeek <= endDate)
            {
                importDates.Add(importWeek);
                importWeek = importWeek.AddDays(7);
            }

            foreach (DateTime week in importDates)
            {
                if (!AlreadyImported(week))
                {
                    foreach (DMDataType type in Enum.GetValues(typeof(DMDataType)))
                    {
                        //Get information
                        string url = String.Format(@"http://torka.unl.edu/DroughtMonitor/Export/?mode=table&aoi={0}&date={1}", type.ToString().ToLower(), week.ToString("yyyyMMdd"));
                        WebClient client = new WebClient();
                        string response = client.DownloadString(url);

                        // split the response into rows based on the new line character
                        List<string> rows = response.Split('\n').ToList<string>();
                        rows.RemoveAt(0); // remove the header row

                        this.WriteData(type, rows, week);
                    }
                }
            } //End foreach week in importDates
             
        } //End ImportDMData (two dates)

        private bool AlreadyImported(DateTime week)
        {
            // Get the ConnectionString by using the configuration ConnectionStrings property to read the connectionString. 
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DMConnection"].ConnectionString))
            {
                conn.Open();
                long dataID = -1;
                using (SqlCommand weekLookup = new SqlCommand(string.Format("select ID from USDMValues where PublishedDate='{0}'", week.ToString("yyyy-MM-dd")), conn))
                using (SqlDataReader reader = weekLookup.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        dataID = reader.GetInt64(0);
                    }
                    reader.Close();
                    if (dataID == -1)
                    {
                        return false;
                    }
                }
                return true;
            }
        } //End AlreadyImported

        private void WriteData(DMDataType type, List<string> rows, DateTime date)
        {
            // Get the ConnectionString by using the configuration ConnectionStrings property to read the connectionString. 
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DMConnection"].ConnectionString))
            {
                conn.Open();
                SqlCommand command = new SqlCommand(null, conn);
                // Create and prepare an SQL statement.
                switch (type)
                {
                    case DMDataType.COUNTY:
                        command.CommandText = "insert into CountyDMValues (PublishedDate, County_ID, DroughtCategory, DroughtValue) values (@date, @placeID, @cat, @value)";
                        command.Parameters.Add("@placeID", SqlDbType.BigInt);
                        break;
                    case DMDataType.STATE:
                        command.CommandText = "insert into StateDMValues (PublishedDate, State_ID, DroughtCategory, DroughtValue) values (@date, @placeID, @cat, @value)";
                        command.Parameters.Add("@placeID", SqlDbType.BigInt);
                        break;
                    case DMDataType.US:
                        command.CommandText = "insert into USDMValues (PublishedDate, DroughtCategory, DroughtValue) values (@date, @cat, @value)";
                        break;
                }
                command.Parameters.Add("@date", SqlDbType.DateTime);
                command.Parameters["@date"].Value = date;
                command.Parameters.Add("@cat", SqlDbType.Int);
                command.Parameters.Add("@value", SqlDbType.Float);
                command.Prepare();  // Calling Prepare after having set the Commandtext and parameters.
                bool wroteUS = false;
                foreach (string line in rows)
                {
                    if (line.Equals("") || wroteUS)
                    {
                        continue;
                    }

                    // split out each column
                    string[] cols = line.Split(',');

                    int offset = 2; // Offset is 2 for the State and US data sets, but is 4 for the County data
                    switch (type)
                    {
                        case DMDataType.COUNTY:
                            //get county ID from col[1]
                            command.Parameters["@placeID"].Value = this.GetCountyID(conn, cols[2], int.Parse(cols[1]), cols[3]);
                            offset = 4;
                            break;
                        case DMDataType.STATE:
                            //get state ID from col[1]
                            command.Parameters["@placeID"].Value = this.GetStateID(conn, cols[1]);
                            break;
                        case DMDataType.US:
                            //only write first line of data for US data
                            wroteUS = true;
                            break;
                    }

                    //Add DM values for all six columns
                    for (int i = 0; i < 6; i++)
                    {
                        // Change parameter values and call ExecuteNonQuery.
                        // Add record for col[i+offset] with category i, week as published date and place ID 
                        command.Parameters["@cat"].Value = i;
                        command.Parameters["@value"].Value = cols[i + offset];
                        command.ExecuteNonQuery();
                    }
                } //End foreach line in rows
            } //End using connection

        } //End WriteData

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

        private DateTime ConvertDateToTuesday(DateTime date)
        {
            TimeSpan span = DateTime.Now - date;
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    return date.AddDays(-5);
                case DayOfWeek.Monday:
                    return date.AddDays(-6);
                case DayOfWeek.Tuesday:
                    if (span.Days > 7) 
                    {
                        return date;
                    }
                    else  // If current week, then go back to the previous Tuesday
                    {
                        return date.AddDays(-7);
                    }
                case DayOfWeek.Wednesday:
                    if (span.Days > 7) 
                    {
                        return date.AddDays(-1);
                    }
                    else // If current week, then go back to the previous Tuesday
                    {
                        return date.AddDays(-8);
                    }
                case DayOfWeek.Thursday:
                    return date.AddDays(-2);
                case DayOfWeek.Friday:
                    return date.AddDays(-3);
                case DayOfWeek.Saturday:
                    return date.AddDays(-4);
                default:
                    return date;
            } //End Switch on Day of Week
        } //End ConvertDateToTuesday

        private void SetDates()
        {
            // Get the ConnectionString by using the configuration ConnectionStrings property to read the connectionString. 
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DMConnection"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand dateLookup = new SqlCommand(string.Format("select top 1 PublishedDate from USDMValues order by PublishedDate"), conn))
                using (SqlDataReader reader = dateLookup.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        this._firstDate = reader.GetDateTime(0).ToString("MM/dd/yyyy");
                    }
                    reader.Close();
                }
                using (SqlCommand dateLookup = new SqlCommand(string.Format("select top 1 PublishedDate from USDMValues order by PublishedDate desc"), conn))
                using (SqlDataReader reader = dateLookup.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        this._lastDate = reader.GetDateTime(0).ToString("MM/dd/yyyy");
                    }
                    reader.Close();
                }
            }
        } //End SetDates
    }
}