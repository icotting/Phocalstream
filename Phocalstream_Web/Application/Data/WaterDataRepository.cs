using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.External;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;


namespace Phocalstream_Web.Application.Data
{
    public enum WaterCodeType
    {
        [Description("Parameter")]
        PARAMETER,
        [Description("Statistic")]
        STATISTIC
    }

    public class WaterDataRepository : IWaterDataRepository
    {

        private string _connectionString;

        public WaterDataRepository(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("The Water Data repository requires an SQL connection");
            }
            _connectionString = connectionString;
        }

        public ICollection<WaterDataValue> Fetch(long stationID, long typeID)
        {
            return FetchByDateRange(stationID, typeID, new DateTime(2009, 1, 1), DateTime.Now);
        }

        public ICollection<WaterDataValue> FetchByDateRange(long stationID, long typeID, DateTime startDate, DateTime endDate)
        {
            List<WaterDataValue> values = new List<WaterDataValue>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand parameterLookup = new SqlCommand(string.Format("select * from WaterValues where Station_ID={0} and DataType_ID={1} and DateTime>={2} and DateTime<={3}", stationID, typeID, startDate, endDate), conn))
                using (SqlDataReader reader = parameterLookup.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        WaterDataValue record = new WaterDataValue();
                        record.ID = reader.GetInt64(0);
                        record.StationID = reader.GetInt64(1);
                        record.DataTypeID = reader.GetInt64(2);
                        record.Date = reader.GetDateTime(3);
                        record.Value = reader.GetFloat(4);
                        values.Add(record);
                    }
                    reader.Close();
                } //End Using SQL Command
            } //End Using SQL Conection
            return values;
        }

        
        public ICollection<WaterParameterCode> FetchParameterCodes()
        {
            List<WaterParameterCode> codes = new List<WaterParameterCode>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
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
                        codes.Add(record);
                    }
                    reader.Close();
                } //End Using SQL Command
            } //End Using SQL Conection
            return codes;
        }

        public ICollection<AvailableWaterDataByStation> FetchCurrentDataTypes()
        {
            List<AvailableWaterDataByStation> stationDataTypes = new List<AvailableWaterDataByStation>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand dataTypeLookup = new SqlCommand(string.Format("select * from AvailableWaterDataTypes"), conn))
                using (SqlDataReader reader = dataTypeLookup.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AvailableWaterDataByStation record = new AvailableWaterDataByStation();
                        record.DataID = reader.GetInt64(0);
                        record.StationID = reader.GetInt64(1);
                        long typeID = reader.GetInt64(2);
                        record.ParameterCode = this.GetCode(typeID, WaterCodeType.PARAMETER);
                        record.StatisticCode = this.GetCode(typeID, WaterCodeType.STATISTIC);
                        record.CurrentLastDate = reader.GetDateTime(4);
                        stationDataTypes.Add(record);
                    }
                    reader.Close();
                }
            }
            return stationDataTypes;
        }

        public WaterStateInfo GetStateInfo(string state)
        {
            WaterStateInfo record = new WaterStateInfo();
            record.ID = -1;
            record.IsImported = false;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand stateLookup = new SqlCommand(string.Format("select * from WaterDataStates where StateCode='{0}'", state), conn))
                using (SqlDataReader reader = stateLookup.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        record.ID = reader.GetInt64(0);
                        record.StateText = reader.GetString(1);
                        record.StateCode = reader.GetString(2);
                        record.IsImported = (bool)reader.GetSqlBoolean(3);
                    }
                    reader.Close();
                }
            }
            return record;
        }
        
        public ICollection<string> FetchCurrentImportedStates()
        {
            List<string> result = new List<string>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand stateLookup = new SqlCommand(string.Format("select StateCode from WaterDataStates where IsImported=1"), conn))
                using (SqlDataReader reader = stateLookup.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(reader.GetString(0).TrimEnd());
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public void DeleteTableData(string tableName)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand command = new SqlCommand(null, conn);
                // Create and execute an SQL statement.
                command.CommandText = string.Format("delete from {0}", tableName);
                command.ExecuteNonQuery();
            }
        }

        public void UpdateStateAsImported(long stateID)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand command = new SqlCommand(null, conn);
                // Create and execute an SQL statement.
                command.CommandText = string.Format("update WaterDataStates set IsImported=1 where ID={0}", stateID);
                command.ExecuteNonQuery();
            }
        }

        public void Add(WaterDataValue waterDataValue)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand command = new SqlCommand(null, conn);
                // Create and prepare an SQL statement.
                command.CommandText = "insert into WaterValues (Station_ID, DataType_ID, DateTime, Value) values (@station, @type, @date, @value)";
                command.Parameters.Add("@station", SqlDbType.BigInt);
                command.Parameters["@station"].Value = waterDataValue.StationID;
                command.Parameters.Add("@type", SqlDbType.BigInt);
                command.Parameters["@type"].Value = waterDataValue.DataTypeID;
                command.Parameters.Add("@date", SqlDbType.DateTime);
                command.Parameters.Add("@value", SqlDbType.Float);
                command.Parameters["@date"].Value = waterDataValue.Date;
                command.Parameters["@value"].Value = waterDataValue.Value;
                command.Prepare();  // Calling Prepare after having set the Commandtext and parameters.
                command.ExecuteNonQuery();
            }
        }

        public long AddWaterStation(string number, string name, float latitude, float longitude, long stateID)
        {
            long stationID = GetStationID(number);
            if (stationID == -1)
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open(); 
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
            }
            return stationID;
        }

        public long AddWaterDataType(long stationID, long parameterID, DateTime fromDate, DateTime toDate)
        {
            long dataTypeID = GetWaterDataTypeID(stationID, parameterID);
            if (dataTypeID == -1)
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
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
            }
            return dataTypeID;
        }

        public void UpdateAvailableDataTypes(AvailableWaterDataByStation dataType)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // Create and execute an SQL statement.
                SqlCommand command = new SqlCommand(null, conn);
                command.CommandText = "update AvailableWaterDataTypes set ToDate=@date where ID=@ID";
                command.Parameters.AddWithValue("@ID", dataType.DataID);
                command.Parameters.Add("@date", SqlDbType.DateTime);
                command.Parameters["@date"].Value = dataType.CurrentLastDate;
                command.ExecuteNonQuery();
            }
        }

        public string GetStationCode(long stationID)
        {
            string stationCode = "";
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand stationLookup = new SqlCommand(string.Format("select StationNumber from WaterStations where ID={0}", stationID), conn))
                using (SqlDataReader reader = stationLookup.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        stationCode = reader.GetString(0).TrimEnd();
                    }
                    reader.Close();
                }
            }
            return stationCode;
        }

        public DateTime GetLastDate()
        {
            DateTime lastDate = new DateTime(2009, 1, 1);
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand dateLookup = new SqlCommand(string.Format("select top 1 ToDate from AvailableWaterDataTypes order by ToDate desc"), conn))
                using (SqlDataReader reader = dateLookup.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        lastDate = reader.GetDateTime(0);
                    }
                    reader.Close();
                }
            }
            return lastDate;
        }


        private long GetStationID(string station)
        {
            long stationID = -1;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand stationLookup = new SqlCommand(string.Format("select ID from WaterStations where StationNumber='{0}'", station), conn))
                using (SqlDataReader reader = stationLookup.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        stationID = reader.GetInt64(0);
                    }
                    reader.Close();
                }
            }
            return stationID;
        }


        private long GetWaterDataTypeID(long stationID, long parameterID)
        {
            long waterDataID = -1;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand dataTypeLookup = new SqlCommand(string.Format("select ID from AvailableWaterDataTypes where Station_ID={0} and Type_ID={1}", stationID, parameterID), conn))
                using (SqlDataReader reader = dataTypeLookup.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        waterDataID = reader.GetInt64(0);
                    }
                    reader.Close();
                }
            }
            return waterDataID;
        }
        
        private string GetCode(long parameterID, WaterCodeType type)
        {
            string code = "";
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand parameterLookup = new SqlCommand(string.Format("select * from WaterDataParameters where ID={0}", parameterID), conn))
                using (SqlDataReader reader = parameterLookup.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        switch (type)
                        {
                            case WaterCodeType.PARAMETER:
                                code = reader.GetString(1).TrimEnd();
                                break;
                            case WaterCodeType.STATISTIC:
                                code = reader.GetString(3).TrimEnd();
                                break;
                            default:
                                return code;
                        }
                    }
                    reader.Close();
                } //End Using SQL Command
            } //End Using SQL Conection
            return code;
        }    

    }
}