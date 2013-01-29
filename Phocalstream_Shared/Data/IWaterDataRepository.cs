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

        //General Water Data
        void DeleteTableData(string tableName);

        //Water State Information
        void UpdateStateAsImported(long stateID);
        ICollection<string> FetchCurrentImportedStates();
        WaterStateInfo GetStateInfo(string state);
        
        //Water Stations
        long AddWaterStation(string number, string name, float latitude, float longitude, long stateID);
        string GetStationCode(long stationID);

        //Water Data Types
        ICollection<AvailableWaterDataByStation> FetchCurrentDataTypes();
        long AddWaterDataType(long stationID, long parameterID, DateTime fromDate, DateTime toDate);
        void UpdateAvailableDataTypes(AvailableWaterDataByStation dataType);
        DateTime GetLastDate();
    }
}
