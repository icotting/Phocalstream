using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class SiteDashboardViewModel
    {
        public CollectionViewModel CollectionViewModel { get; set; }
        public List<SiteYearModel> Years { get; set; }
        public List<Tuple<string, int, long>> Tags { get; set; }
        public PhotoFrequencyData PhotoFrequency { get; set; }
        public DmData DroughtMonitorData { get; set; }
        public WaterFlowData WaterData { get; set; }
    }

    public class SiteYearModel
    {
        public string Year { get; set; }
        public int PhotoCount { get; set; }
        public long CoverPhotoID { get; set; }
    }

    public class PhotoFrequencyData
    {
        public string SiteName { get; set; }
        public String FrequencyDataValues { get; set; }
        public DateTime StartDate { get; set; }
    }

}