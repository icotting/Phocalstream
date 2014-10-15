using Microsoft.Practices.Unity;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.External;
using Phocalstream_Shared.Service;
using Phocalstream_Web.Models.Api;
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

        [HttpPost]
        [ActionName("dmcountyweek")]
        public IDictionary<string, DroughtMonitorWeek> GetWeekDMData(DroughtMonitorRequest DmRequest)
        {
            USCounty county = DmRepo.GetCountyForFips(DmRequest.CountyFips);
            Dictionary<string, DroughtMonitorWeek> results = new Dictionary<string, DroughtMonitorWeek>();
            results.Add("COUNTY", DmRepo.FindBy(county, DmRequest.DmWeek).FirstOrDefault());
            results.Add("STATE", DmRepo.FindBy(county.State, DmRequest.DmWeek).First());
            results.Add("US", DmRepo.FindUS(DmRequest.DmWeek).FirstOrDefault());

            return results;
        }
    }
}
