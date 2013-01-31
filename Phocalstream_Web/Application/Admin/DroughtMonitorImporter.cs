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
using Phocalstream_Shared.Data.Model.External;
using Phocalstream_Shared.Data;

namespace Phocalstream_Web.Application.Admin
{
                
    public class DroughtMonitorImporter
    {
        [Microsoft.Practices.Unity.Dependency]
        public IDroughtMonitorRepository DmRepository { get; set; }

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
            startDate = DroughtMonitorWeek.ConvertDateToTuesday(startDate);
            endDate = DroughtMonitorWeek.ConvertDateToTuesday(endDate);

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
            List<DroughtMonitorWeek> importedList =  DmRepository.FindUS(week, 0).ToList<DroughtMonitorWeek>();

            return (importedList.Count > 0);
            
            /*
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
            } */
        } //End AlreadyImported

        private void WriteData(DMDataType type, List<string> rows, DateTime date)
        {
            DroughtMonitorWeek dmWeek = new DroughtMonitorWeek();
            dmWeek.Type = type;
            dmWeek.Week = date;

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
                        dmWeek.County.ID = 0;
                        dmWeek.County.Name = cols[2];
                        dmWeek.County.Fips = int.Parse(cols[1]);
                        dmWeek.County.State.Name = cols[3];
                        dmWeek.State.Name = cols[3];
                        //command.Parameters["@placeID"].Value = this.GetCountyID(conn, cols[2], int.Parse(cols[1]), cols[3]);
                        offset = 4;
                        break;
                    case DMDataType.STATE:
                        //get state ID from col[1]
                        dmWeek.State.ID = 0;
                        dmWeek.State.Name = cols[1];
                        //command.Parameters["@placeID"].Value = this.GetStateID(conn, cols[1]);
                        break;
                    case DMDataType.US:
                        //only write first line of data for US data
                        wroteUS = true;
                        break;
                }

                //Add DM values for all six columns
                for (int i = 0; i < 6; i++)
                {
                    // Set value for col[i+offset] with category i 
                    dmWeek[i] = float.Parse(cols[i + offset]);
                }

                DmRepository.Add(dmWeek);
            } //End foreach line in rows

        } //End WriteData

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