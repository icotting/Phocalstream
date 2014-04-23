using Phocalstream_Shared.Data.Model.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data
{
    public interface IWaterDataRepository
    {
        //Water Data Values
        ICollection<WaterDataValue> Fetch(long stationID, long typeID);
        ICollection<WaterDataValue> FetchByDateRange(long stationID, long typeID, DateTime startDate, DateTime endDate);
        void Add(WaterDataValue waterDataValue); 
        
        //Water Parameter Codes
        ICollection<WaterParameterCode> FetchParameterCodes();
        WaterParameterCode GetParameterCodeInfoFromDataType(long dataTypeID);

        //General Water Data
        void DeleteTableData(string tableName);

        //Water State Information
        void UpdateStateAsImported(long stateID);
        ICollection<string> FetchCurrentImportedStates();
        WaterStateInfo GetStateInfo(string state);
        
        //Water Stations
        long AddWaterStation(string number, string name, float latitude, float longitude, long stateID);
        string GetStationCode(long stationID);
        ICollection<WaterStation> GetClosestStations(double siteLatitude, double siteLongitude, int range);
        WaterStation GetStationInfo(long stationID);

        //Water Data Types
        ICollection<AvailableWaterDataByStation> FetchCurrentDataTypes();
        ICollection<AvailableWaterDataByStation> FetchBestDataTypesForStationDate(ICollection<WaterStation> stationIDs, DateTime imageDate);
        long AddWaterDataType(long stationID, long parameterID, DateTime fromDate, DateTime toDate);
        void UpdateAvailableDataTypes(AvailableWaterDataByStation dataType);
        DateTime GetLastDate();
    }
}
