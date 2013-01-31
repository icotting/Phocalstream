using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data.Model.External
{
    public class WaterParameterCode
    {
        public long ParameterID { get; set; }
        public string ParameterCode { get; set; }
        public string ParameterDesc { get; set; }
        public string StatisticCode { get; set; }
        public string StatisticDesc { get; set; }
        public string UnitOfMeasureDesc { get; set; }
    }
}
