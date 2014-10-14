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
        public IDroughtMonitorService DmService { get; set; }

        [HttpPost]
        [ActionName("test")]
        public string testMethod(string test)
        {
            return "hello " + test;
        }

        [HttpPost] // has to be post to support large parameter lists
        [ActionName("timelapsedm")]
        public IEnumerable<DroughtMonitorWeek> GetDmDataForIdList(TimeLapseDataRequest model)
        {
            return DmService.FindForSequence(model.IdList.Split(',').Select(i => Convert.ToInt64(i)).ToArray<long>(), model.CountyFips);
        }
    }
}
