using Phocalstream_Shared.Data;
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
        [Microsoft.Practices.Unity.Dependency]
        public IWaterDataRepository WaterRepository { get; set; }

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
            this._parameterCodes = WaterRepository.FetchParameterCodes().ToList<WaterParameterCode>();
            this._lastDate = WaterRepository.GetLastDate();
        }

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
            //using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["WaterDBConnection"].ConnectionString))

            List<AvailableWaterDataByStation> stationDataTypes = WaterRepository.FetchCurrentDataTypes().ToList<AvailableWaterDataByStation>();
            //Loop through Available Water Data Types using station, parameter, and statistic info
            foreach (AvailableWaterDataByStation dataType in stationDataTypes)
            {
                //Get data from current last date to current date
                //Write values and keep track of new greatest date
                DateTime newLastDate = this.UpdateWaterValues(dataType, DateTime.Now);
                //If added values and have a new greatest date, update the Available Water Data Type
                if (newLastDate.CompareTo(dataType.CurrentLastDate) > 0)
                {
                    //Update date in DB
                    dataType.CurrentLastDate = newLastDate;
                    WaterRepository.UpdateAvailableDataTypes(dataType);
                }
            }
            //Reset the instance's last date value
            this._lastDate = WaterRepository.GetLastDate();
        } //End UpdateWaterDataToCurrent

        private DateTime UpdateWaterValues(AvailableWaterDataByStation dataType, DateTime endDate)
        {
            DateTime resultDate = dataType.CurrentLastDate;
            //Get data from current last date (plus 1) to current date
            //Get information
            string url = String.Format(@"http://waterservices.usgs.gov/nwis/dv/?format=rdb&sites={0}&parameterCd={1}&statCd={2}&startDT={3}&endDT={4}",
                WaterRepository.GetStationCode(dataType.StationID), dataType.ParameterCode, dataType.StatisticCode, dataType.CurrentLastDate.AddDays(1).ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
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
                resultDate = this.WriteDataValues(rows, dataType.StationID, dataType.DataID);
                if (resultDate.CompareTo(new DateTime(2009, 1, 1)) <= 0)
                {
                    resultDate = dataType.CurrentLastDate;
                }
            }

            return resultDate;
        } //End UpdateWaterValues

        private void ResetWaterData()
        {
            this.RemoveCurrentWaterData();
            List<string> currentStates = WaterRepository.FetchCurrentImportedStates().ToList<string>();
            //Update the instance's Parameter Codes just in case they are resetting the data because they added a new parameter
            this._parameterCodes = WaterRepository.FetchParameterCodes().ToList<WaterParameterCode>();
            foreach (string state in currentStates)
            {
                //Call the importer by state in reset mode
                this.ImportWaterDataByState(state, new DateTime(2009, 1, 1), true);
            }
            //Reset the instance's last date value
            this._lastDate = WaterRepository.GetLastDate();
        } //End ReseWaterData

        private void RemoveCurrentWaterData()
        {
            //Delete Water Values
            WaterRepository.DeleteTableData("WaterValues");
            //Delete Water Data Types
            WaterRepository.DeleteTableData("AvailableWaterDataTypes");
            //Delete Water Stations
            WaterRepository.DeleteTableData("WaterStations");
        } //End RemoveCurrentWaterData

        private void ImportWaterDataByState(string state, DateTime startDate, bool reset)
        {
            WaterStateInfo stateInfo = WaterRepository.GetStateInfo(state);
            if (!stateInfo.IsImported || reset)
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
                this.WriteData(rows, stateInfo.ID, startDate);
                //Reset the instance's last date value
                this._lastDate = WaterRepository.GetLastDate();

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
            DateTime tempDate = new DateTime(2011, 12, 31);

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
                    long stationID = WaterRepository.AddWaterStation(cols[1], cols[2], float.Parse(cols[4]), float.Parse(cols[5]), stateID);

                    //Write Water Data Types
                    long waterDataTypeID = WaterRepository.AddWaterDataType(stationID, this.GetParameterID(cols[12], cols[13]), DateTime.Parse(cols[20]), DateTime.Parse(cols[21]));

                    //Import and write data values for the station and data type
                    this.ImportAndWriteDataValues(cols[1], cols[12], cols[13], stationID, waterDataTypeID, startDate);
                }

            } //End foreach line in rows
            //Update State as having been imported
            WaterRepository.UpdateStateAsImported(stateID);
        } //End WriteData

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

        private void ImportAndWriteDataValues(string stationCode, string parameterCode, string statisticCode, long stationID, long waterDataTypeID, DateTime startDate)
        {
            AvailableWaterDataByStation dataType = new AvailableWaterDataByStation();
            dataType.CurrentLastDate = startDate;
            dataType.DataID = waterDataTypeID;
            dataType.ParameterCode = parameterCode;
            //dataType.StationCode = stationCode;
            dataType.StationID = stationID;
            dataType.StatisticCode = statisticCode;

            //Get information
            DateTime newLastDate = this.UpdateWaterValues(dataType, DateTime.Now);

        } //End ImportAndWriteDataValues

        private DateTime WriteDataValues(List<string> rows, long stationID, long waterDataTypeID)
        {
            DateTime resultDate = new DateTime(2009, 1, 1);
            float value = -999999;

            WaterDataValue waterValue = new WaterDataValue();
            waterValue.StationID = stationID;
            waterValue.DataTypeID = waterDataTypeID;

            foreach (string line in rows)
            {
                if (line.Equals(""))
                {
                    continue;
                }

                // split out each column
                string[] cols = line.Split('\t');

                waterValue.Date = DateTime.Parse(cols[2]);
                resultDate = DateTime.Parse(cols[2]);

                if (float.TryParse(cols[3], out value))
                {
                    waterValue.Value = value;
                }
                else
                {
                    waterValue.Value = -999999;
                }
                WaterRepository.Add(waterValue);
            } //End foreach line in rows

            return resultDate;
        } //End WriteDataValues

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

    }
}
