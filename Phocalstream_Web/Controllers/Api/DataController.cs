using Microsoft.Practices.Unity;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.External;
using Phocalstream_Shared.Service;
using Phocalstream_Web.Models.Api;
using Phocalstream_Web.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Phocalstream_Web.Controllers.Api
{
    public class DataController : ApiController
    {

        [Dependency]
        public IDroughtMonitorRepository DmRepo { get; set; }

        [Dependency]
        public IWaterDataRepository WaterRepo { get; set; }

        [HttpPost]
        [ActionName("timelapseweek")]
        public TimelapseDataWeek GetTimelapseData(TimelapseDataRequest request)
        {
            USCounty county = DmRepo.GetCountyForFips(request.CountyFips);
            Dictionary<string, DroughtMonitorWeek> results = new Dictionary<string, DroughtMonitorWeek>();

            DateTime date = Convert.ToDateTime(request.DmWeek);

            results.Add("COUNTY", DmRepo.FindBy(county, date).FirstOrDefault());
            results.Add("STATE", DmRepo.FindBy(county.State, date).First());
            results.Add("US", DmRepo.FindUS(date).FirstOrDefault());

            ICollection<AvailableWaterDataByStation> types = WaterRepo.FetchBestDataTypesForStationDate(WaterRepo.GetClosestStations(request.Latitude, request.Longitude,
                1), date);
            AvailableWaterDataByStation discharge = types.Where(t => t.ParameterCode == "00060").FirstOrDefault();
            double averageDischarge = 0;

            if (discharge != null)
            {
                double total = 0;
                ICollection<WaterDataValue> values = WaterRepo.FetchByDateRange(discharge.StationID, discharge.DataID, date, date.AddDays(7));
                foreach (WaterDataValue wvalue in values)
                {
                    if (wvalue.Value != -999999)
                    {
                        total += wvalue.Value;
                    }
                }
                averageDischarge = total / (double)values.Count;
            }

            return new TimelapseDataWeek { DMData = results, AverageDischarge = averageDischarge };
        }
    }
}
