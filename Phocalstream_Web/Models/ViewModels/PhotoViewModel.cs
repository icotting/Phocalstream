using Phocalstream_Shared.Data.Model.External;
using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class PhotoViewModel
    {
        public Photo Photo { get; set; }
        public string ImageUrl { get; set; }
        public string PhotoDate { get; set; }
        public string PhotoTime { get; set; }
        public string SiteCoords { get; set; }
        public DmData DroughtMonitorData { get; set; }
        public WaterFlowData WaterData { get; set; }
    }

    public class DmData
    {
        public long PhotoID { get; set; }
        public DroughtMonitorWeek DMValues { get; set; }
        public DroughtMonitorWeek PreviousWeekValues { get; set; }
        public DroughtMonitorWeek PreviousMonthValues { get; set; }
        public string DataWeek { get; set; }
        public string DisplayDate { get; set; }
    }

    public class DmMapData
    {
        public string DataWeek { get; set; }
    }

    public class WaterFlowData
    {
        public long PhotoID { get; set; }
        public WaterStation ClosestStation { get; set; }
        public AvailableWaterDataByStation DataTypes { get; set; }
        public ICollection<WaterDataValue> WaterDataValues { get; set; }
        public WaterParameterCode ParameterInfo { get; set; }
        public String chartDataValues { get; set; }
    }
}