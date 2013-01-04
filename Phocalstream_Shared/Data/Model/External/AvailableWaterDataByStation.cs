using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data.Model.External
{
    public class AvailableWaterDataByStation
    {
        public long DataID { get; set; }
        public long StationID { get; set; }
        public string ParameterCode { get; set; }
        public string StatisticCode { get; set; }
        public DateTime CurrentLastDate { get; set; }
    }
}
