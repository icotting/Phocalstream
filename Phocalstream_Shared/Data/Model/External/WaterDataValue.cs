using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data.Model.External
{
    public class WaterDataValue
    {
        public long ID { get; set; }
        public long StationID { get; set; }
        public long DataTypeID { get; set; }
        public DateTime Date { get; set; }
        public double Value { get; set; }
    }

    public class WaterStation
    {
        public long ID { get; set; }
        public string StationNumber { get; set; }
        public string StationName { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public long StateID { get; set; }
    }

    public class WaterStateInfo
    {
        public long ID { get; set; }
        public string StateText { get; set; }
        public string StateCode { get; set; }
        public bool IsImported { get; set; }
    }
}
