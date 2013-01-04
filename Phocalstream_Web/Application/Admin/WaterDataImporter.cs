using Phocalstream_Shared.Data.Model.External;
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

namespace Phocalstream_Web.Application.Admin
{
    public class WaterDataImporter
    {
        private static WaterDataImporter _instance;

        private bool _importRunning;
        private List<WaterParameterCode> _parameterCodes;
        private DateTime _lastDate;

        public bool ImportRunning
        {
            get { return _importRunning; }
        }

        public string LastDate
        {
            get { return _lastDate.ToString("MM/dd/yyyy"); }
        }

        private WaterDataImporter()
        {
            this._importRunning = false;
            this.LoadParameterCodes();
            this.SetDates();
        }

        private void LoadParameterCodes()
        {
            this._parameterCodes = new List<WaterParameterCode>();
            // Get the ConnectionString by using the configuration ConnectionStrings property to read the connectionString. 
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["WaterDBConnection"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand parameterLookup = new SqlCommand(string.Format("select * from WaterDataParameters"), conn))
                using (SqlDataReader reader = parameterLookup.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        WaterParameterCode record = new WaterParameterCode();
                        record.ParameterID = reader.GetInt64(0);
                        record.ParameterCode = reader.GetString(1).TrimEnd();
                        record.ParameterDesc = reader.GetString(2);
                        record.StatisticCode = reader.GetString(3).TrimEnd();
                        record.StatisticDesc = reader.GetString(4);
                        record.UnitOfMeasureDesc = reader.GetString(5);
                        this._parameterCodes.Add(record);
                    }
                    reader.Close();
                } //End Using SQL Command
            } //End Using SQL Conection
        } //End LoadParameterCodes

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static WaterDataImporter getInstance()
        {
            if (_instance == null)
            {
                _instance = new WaterDataImporter();
            }
            return _instance;
        } //End getInstance

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RunWaterImport(string state)
        {
            if (!ImportRunning)
            {
                new Task(() =>
                {
                    this._importRunning = true;
                    this.ImportWaterDataByState(state, new DateTime(2009, 1, 1), false);
                    this._importRunning = false;
                }).Start();
            }
        } //End RunWaterImport (state)

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void UpdateWaterData()
        {
            if (!ImportRunning)
            {
                new Task(() =>
                {
                    this._importRunning = true;
                    this.UpdateWaterDataToCurrent();
                    this._importRunning = false;
                }).Start();
            }
        } //End UpdateWaterData

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ResetAllWaterData()
        {
            if (!ImportRunning)
            {
                new Task(() =>
                {
                    this._importRunning = true;
                    this.ResetWaterData();
                    this._importRunning = false;
                }).Start();
            }
        } //End ResetAllWaterData

        private void UpdateWaterDataToCurrent()
        {
            // Get the ConnectionString by using the configuration ConnectionStrings property to read the connectionString. 
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["WaterDBConnection"].ConnectionString))
            {
                conn.Open();
                List<AvailableWaterDataByStation> stationDataTypes = this.LoadCurrentDataTypes(conn);
                //Loop through Available Water Data Types using station, parameter, and statistic info
                foreach (AvailableWaterDataByStation dataType in stationDataTypes)
                {
                    //Get data from current last date to current date
                    //Write values and keep track of new greatest date
                    DateTime newLastDate = this.UpdateWaterValues(conn, dataType, DateTime.Now);
                    //If added values and have a new greatest date, update the Available Water Data Type
                    if (newLastDate.CompareTo(dataType.CurrentLastDate) > 0)
                    {
                        //Update date in DB
                        dataType.CurrentLastDate = newLastDate;
                        this.UpdateAvailableDataTypes(conn, dataType);
                    }
                }
                //Reset the instance's last date value
                this.SetDates();
            }
        } //End UpdateWaterDataToCurrent

        private DateTime UpdateWaterValues(SqlConnection conn, AvailableWaterDataByStation dataType, DateTime endDate)
        {
            DateTime resultDate = dataType.CurrentLastDate;
            //Get data from current last date (plus 1) to current date
            //Get information
            string url = String.Format(@"http://waterservices.usgs.gov/nwis/dv/?format=rdb&sites={0}&parameterCd={1}&statCd={2}&startDT={3}&endDT={4}",
                this.GetStationCode(conn, dataType.StationID), dataType.ParameterCode, dataType.StatisticCode, dataType.CurrentLastDate.AddDays(1).ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
            WebClient client = new WebClient();
            string response = client.DownloadString(url);

            // split the response into rows based on the new line character
            List<string> rows = response.Split('\n').ToList<string>();
            while (rows[0].StartsWith("#"))
            {
                rows.RemoveAt(0); // remove the header comment rows
            }
            rows.RemoveAt(0); // remove the header row
            rows.RemoveAt(0); // remove the definition row
            //Write values and keep track of new greatest date
            if (rows.Count != 0)
            {
                resultDate = this.WriteDataValues(conn, rows, dataType.StationID, dataType.DataID);
                if (resultDate.CompareTo(new DateTime(2009, 1, 1)) <= 0)
                {
                    resultDate = dataType.CurrentLastDate;
                }
            }

            return resultDate;
        } //End UpdateWaterValues

        private List<AvailableWaterDataByStation> LoadCurrentDataTypes(SqlConnection conn)
        {
            List<AvailableWaterDataByStation> stationDataTypes = new List<AvailableWaterDataByStation>();
            using (SqlCommand dataTypeLookup = new SqlCommand(string.Format("select * from AvailableWaterDataTypes"), conn))
            using (SqlDataReader reader = dataTypeLookup.ExecuteReader())
            {
                while (reader.Read())
                {
                    AvailableWaterDataByStation record = new AvailableWaterDataByStation();
                    record.DataID = reader.GetInt64(0);
                    record.StationID = reader.GetInt64(1);
                    long typeID = reader.GetInt64(2);
                    record.ParameterCode = this.GetParameterCode(typeID);
                    record.StatisticCode = this.GetStatisticCode(typeID);
                    record.CurrentLastDate = reader.GetDateTime(4);
                    stationDataTypes.Add(record);
                }
                reader.Close();
            }

            return stationDataTypes;
        } //End LoadCurrentDataTypes

        private void ResetWaterData()
        {
            // Get the ConnectionString by using the configuration ConnectionStrings property to read the connectionString. 
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["WaterDBConnection"].ConnectionString))
            {
                conn.Open();
                this.RemoveCurrentWaterData(conn);
                List<string> currentStates = this.GetCurrentImportedStates(conn);
                //Update the instance's Parameter Codes just in case they are resetting the data because they added a new parameter
                this.LoadParameterCodes();
                foreach (string state in currentStates)
                {
                    //Call the importer by state in reset mode
                    this.ImportWaterDataByState(state, new DateTime(2009, 1, 1), true);
                }
            }
            //Reset the instance's last date value
            this.SetDates();
        } //End ReseWaterData

        private List<string> GetCurrentImportedStates(SqlConnection conn)
        {
            List<string> result = new List<string>();
            using (SqlCommand stateLookup = new SqlCommand(string.Format("select StateCode from WaterDataStates where IsImported=1"), conn))
            using (SqlDataReader reader = stateLookup.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(reader.GetString(0).TrimEnd());
                }
                reader.Close();
            }
            return result;
        } //End GetCurrentImportedStates

        private void RemoveCurrentWaterData(SqlConnection conn)
        {
            //Delete Water Values
            this.DeleteWaterData(conn, "WaterValues");
            //Delete Water Data Types
            this.DeleteWaterData(conn, "AvailableWaterDataTypes");
            //Delete Water Stations
            this.DeleteWaterData(conn, "WaterStations");
        } //End RemoveCurrentWaterData

        private void DeleteWaterData(SqlConnection conn, string tableName)
        {
            SqlCommand command = new SqlCommand(null, conn);
            // Create and execute an SQL statement.
            command.CommandText = string.Format("delete from {0}", tableName);
            command.ExecuteNonQuery();
        } //End DeleteWaterData

        private void ImportWaterDataByState(string state, DateTime startDate, bool reset)
        {
            long stateID;
            if (!AlreadyImported(state, out stateID) || reset)
            {

                //Get information
                string url = String.Format(@"http://waterservices.usgs.gov/nwis/site/?format=rdb&stateCd={0}&outputDataTypeCd=dv&startDT={1}&parameterCd={2}", state, startDate.ToString("yyyy-MM-dd"), ParametersToStringForRead());
                WebClient client = new WebClient();
                string response = client.DownloadString(url);

                // split the response into rows based on the new line character
                List<string> rows = response.Split('\n').ToList<string>();
                while (rows[0].StartsWith("#"))
                {
                    rows.RemoveAt(0); // remove the header comment rows
                }
                rows.RemoveAt(0); // remove the header row
                rows.RemoveAt(0); // remove the definition row
                this.WriteData(rows, stateID, startDate);
                //Reset the instance's last date value
                this.SetDates();

            }
        } //End ImportWaterDataByState

        private string ParametersToStringForRead()
        {
            string result = "";
            char[] charsToTrim = { ',' };
            foreach (WaterParameterCode record in this._parameterCodes)
            {
                result += record.ParameterCode + ",";
            }
            return result.TrimEnd(charsToTrim);
        } //End ParametersToStringForRead

        private void WriteData(List<string> rows, long stateID, DateTime startDate)
        {
            // Get the ConnectionString by using the configuration ConnectionStrings property to read the connectionString. 
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["WaterDBConnection"].ConnectionString))
            {
                DateTime tempDate = new DateTime(2011, 12, 31);
                conn.Open();
                foreach (string line in rows)
                {
                    if (line.Equals(""))
                    {
                        continue;
                    }

                    // split out each column
                    string[] cols = line.Split('\t');

                    if (ValidRow(cols[12], cols[13], cols[15], cols[21], startDate))
                    {
                        //Write Water Station
                        long stationID = AddWaterStation(conn, cols[1], cols[2], float.Parse(cols[4]), float.Parse(cols[5]), stateID);

                        //Write Water Data Types
                        long waterDataTypeID = AddWaterDataType(conn, stationID, this.GetParameterID(cols[12], cols[13]), DateTime.Parse(cols[20]), DateTime.Parse(cols[21]));

                        //Import and write data values for the station and data type
                        this.ImportAndWriteDataValues(conn, cols[1], cols[12], cols[13], stationID, waterDataTypeID, startDate);
                    }

                } //End foreach line in rows
                //Update State as having been imported
                this.UpdateStateAsImported(conn, stateID);
            } //End using connection
        } //End WriteData

        private void UpdateStateAsImported(SqlConnection conn, long stateID)
        {
            SqlCommand command = new SqlCommand(null, conn);
            // Create and execute an SQL statement.
            command.CommandText = string.Format("update WaterDataStates set IsImported=1 where ID={0}", stateID);
            command.ExecuteNonQuery();
        } // End UpdateStateAsImported

        private bool ValidRow(string paraCode, string statCode, string additionalMeasure, string dataEndDate, DateTime startDate)
        {
            long parameterID = GetParameterID(paraCode, statCode);

            if (parameterID != -1)
            {
                if (additionalMeasure.Equals("") && (DateTime.Parse(dataEndDate).CompareTo(startDate) > 0))
                {
                    return true;
                }
            }
            return false;
        } // End ValidRow

        private void ImportAndWriteDataValues(SqlConnection conn, string stationCode, string parameterCode, string statisticCode, long stationID, long waterDataTypeID, DateTime startDate)
        {
            AvailableWaterDataByStation dataType = new AvailableWaterDataByStation();
            dataType.CurrentLastDate = startDate;
            dataType.DataID = waterDataTypeID;
            dataType.ParameterCode = parameterCode;
            //dataType.StationCode = stationCode;
            dataType.StationID = stationID;
            dataType.StatisticCode = statisticCode;

            //Get information
            DateTime newLastDate = this.UpdateWaterValues(conn, dataType, DateTime.Now);

        } //End ImportAndWriteDataValues

        private DateTime WriteDataValues(SqlConnection conn, List<string> rows, long stationID, long waterDataTypeID)
        {
            DateTime resultDate = new DateTime(2009, 1, 1);
            float value = -999999;

            SqlCommand command = new SqlCommand(null, conn);
            // Create and prepare an SQL statement.
            command.CommandText = "insert into WaterValues (Station_ID, DataType_ID, DateTime, Value) values (@station, @type, @date, @value)";
            command.Parameters.Add("@station", SqlDbType.BigInt);
            command.Parameters["@station"].Value = stationID;
            command.Parameters.Add("@type", SqlDbType.BigInt);
            command.Parameters["@type"].Value = waterDataTypeID;
            command.Parameters.Add("@date", SqlDbType.DateTime);
            command.Parameters.Add("@value", SqlDbType.Float);
            command.Prepare();  // Calling Prepare after having set the Commandtext and parameters.
            foreach (string line in rows)
            {
                if (line.Equals(""))
                {
                    continue;
                }

                // split out each column
                string[] cols = line.Split('\t');

                command.Parameters["@date"].Value = DateTime.Parse(cols[2]);
                resultDate = DateTime.Parse(cols[2]);

                if (float.TryParse(cols[3], out value))
                {
                    command.Parameters["@value"].Value = value;
                }
                else
                {
                    command.Parameters["@value"].Value = -999999;
                }
                command.ExecuteNonQuery();

            } //End foreach line in rows

            return resultDate;
        } //End WriteDataValues


        private bool AlreadyImported(string state, out long stateID)
        {
            // Get the ConnectionString by using the configuration ConnectionStrings property to read the connectionString. 
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["WaterDBConnection"].ConnectionString))
            {
                conn.Open();
                stateID = -1;
                using (SqlCommand stateLookup = new SqlCommand(string.Format("select * from WaterDataStates where StateCode='{0}'", state), conn))
                using (SqlDataReader reader = stateLookup.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        stateID = reader.GetInt64(0);
                        return (bool)reader.GetSqlBoolean(3);
                    }
                    reader.Close();
                    if (stateID == -1)
                    {
                        return false;
                    }
                }
            }
            return false;
        } //End AlreadyImported

        private long AddWaterStation(SqlConnection conn, string number, string name, float latitude, float longitude, long stateID)
        {
            long stationID = GetStationID(conn, number);
            if (stationID == -1)
            {
                SqlCommand command = new SqlCommand(null, conn);
                // Create and prepare an SQL statement.
                command.CommandText = "insert into WaterStations (StationNumber, StationName, Latitude, Longitude, State_ID) values (@number, @name, @lat, @long, @state)";
                command.Parameters.Add("@number", SqlDbType.NVarChar, 50);
                command.Parameters.Add("@name", SqlDbType.NVarChar, 50);
                command.Parameters.Add("@lat", SqlDbType.Float);
                command.Parameters.Add("@long", SqlDbType.Float);
                command.Parameters.Add("@state", SqlDbType.BigInt);
                command.Prepare();  // Calling Prepare after having set the Commandtext and parameters.

                command.Parameters["@number"].Value = number;
                command.Parameters["@name"].Value = name;
                command.Parameters["@lat"].Value = latitude;
                command.Parameters["@long"].Value = longitude;
                command.Parameters["@state"].Value = stateID;
                command.ExecuteNonQuery();
                // Read the new ID back to return
                command.Parameters.Clear();
                command.CommandText = "SELECT @@IDENTITY";
                return Convert.ToInt64(command.ExecuteScalar());
            }

            return stationID;
        } //End AddWaterStation

        private long AddWaterDataType(SqlConnection conn, long stationID, long parameterID, DateTime fromDate, DateTime toDate)
        {
            long dataTypeID = GetWaterDataTypeID(conn, stationID, parameterID);
            if (dataTypeID == -1)
            {
                SqlCommand command = new SqlCommand(null, conn);
                // Create and prepare an SQL statement.
                command.CommandText = "insert into AvailableWaterDataTypes (Station_ID, Type_ID, FromDate, ToDate) values (@station, @dataType, @fromDate, @toDate)";
                command.Parameters.AddWithValue("@station", stationID);
                //command.Parameters.Add("@station", SqlDbType.Float);
                command.Parameters.Add("@dataType", SqlDbType.BigInt);
                command.Parameters["@dataType"].Value = parameterID;
                command.Parameters.Add("@fromDate", SqlDbType.DateTime);
                command.Parameters["@fromDate"].Value = fromDate;
                command.Parameters.Add("@toDate", SqlDbType.DateTime);
                command.Parameters["@toDate"].Value = toDate;
                command.ExecuteNonQuery();

                // Read the new ID back to return
                command.Parameters.Clear();
                command.CommandText = "SELECT @@IDENTITY";
                return Convert.ToInt64(command.ExecuteScalar());
            }
            return dataTypeID;
        } //End AddWaterDataType

        private void UpdateAvailableDataTypes(SqlConnection conn, AvailableWaterDataByStation dataType)
        {
            // Create and execute an SQL statement.
            SqlCommand command = new SqlCommand(null, conn);
            command.CommandText = "update AvailableWaterDataTypes set ToDate=@date where ID=@ID";
            command.Parameters.AddWithValue("@ID", dataType.DataID);
            command.Parameters.Add("@date", SqlDbType.DateTime);
            command.Parameters["@date"].Value = dataType.CurrentLastDate;
            command.ExecuteNonQuery();

        } //End UpdateAvailableDataTypes

        private long GetStationID(SqlConnection conn, string station)
        {
            long stationID = -1;
            using (SqlCommand stationLookup = new SqlCommand(string.Format("select ID from WaterStations where StationNumber='{0}'", station), conn))
            using (SqlDataReader reader = stationLookup.ExecuteReader())
            {
                if (reader.Read())
                {
                    stationID = reader.GetInt64(0);
                }
                reader.Close();
            }
            return stationID;
        } //End GetStationID

        private string GetStationCode(SqlConnection conn, long stationID)
        {
            string stationCode = "";
            using (SqlCommand stationLookup = new SqlCommand(string.Format("select StationNumber from WaterStations where ID={0}", stationID), conn))
            using (SqlDataReader reader = stationLookup.ExecuteReader())
            {
                if (reader.Read())
                {
                    stationCode = reader.GetString(0).TrimEnd();
                }
                reader.Close();
            }
            return stationCode;
        } //End GetStationCode

        private long GetWaterDataTypeID(SqlConnection conn, long stationID, long parameterID)
        {
            long dataTypeID = -1;
            using (SqlCommand dataTypeLookup = new SqlCommand(string.Format("select ID from AvailableWaterDataTypes where Station_ID={0} and Type_ID={1}", stationID, parameterID), conn))
            using (SqlDataReader reader = dataTypeLookup.ExecuteReader())
            {
                if (reader.Read())
                {
                    stationID = reader.GetInt64(0);
                }
                reader.Close();
            }
            return dataTypeID;
        } //End GetWaterDataTypeID

        private long GetParameterID(string dataType, string statType)
        {
            long parameterID = -1;
            foreach (WaterParameterCode record in this._parameterCodes)
            {
                if (record.ParameterCode.Equals(dataType) && record.StatisticCode.Equals(statType))
                {
                    return record.ParameterID;
                }
            }
            return parameterID;
        } //End GetParameterID

        private string GetParameterCode(long parameterID)
        {
            string parameterCode = "";
            foreach (WaterParameterCode record in this._parameterCodes)
            {
                if (record.ParameterID == parameterID)
                {
                    return record.ParameterCode;
                }
            }
            return parameterCode;
        } //End GetParameterCode

        private string GetStatisticCode(long parameterID)
        {
            string statisticCode = "";
            foreach (WaterParameterCode record in this._parameterCodes)
            {
                if (record.ParameterID == parameterID)
                {
                    return record.StatisticCode;
                }
            }
            return statisticCode;
        } //End GetStatisticCode

        private void SetDates()
        {
            this._lastDate = new DateTime(2009, 1, 1);
            // Get the ConnectionString by using the configuration ConnectionStrings property to read the connectionString. 
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["WaterDBConnection"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand dateLookup = new SqlCommand(string.Format("select top 1 ToDate from AvailableWaterDataTypes order by ToDate desc"), conn))
                using (SqlDataReader reader = dateLookup.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        this._lastDate = reader.GetDateTime(0);
                    }
                    reader.Close();
                }
            }
        } //End SetDates

    }
}
