using Microsoft.Practices.Unity;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.External;
using Phocalstream_Shared.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Service.Service
{
    public class DroughtMonitorService : IDroughtMonitorService
    {
        [Dependency]
        public IDroughtMonitorRepository DmRepository { get; set; }

        [Dependency]
        public IPhotoRepository PhotoRepository { get; set; }

        public ICollection<DroughtMonitorWeek> FindForSequence(long[] ids, int countyFips)
        {
            List<DroughtMonitorWeek> weeks = new List<DroughtMonitorWeek>();
            IEnumerable<DateTime> dates = PhotoRepository.FindDmDatesForPhotos(ids);

            foreach (DateTime date in dates)
            {
                weeks.Add(DmRepository.FindBy(DmRepository.GetCountyForFips(countyFips), date).FirstOrDefault());
            }
            return weeks;
        }

        public ICollection<DroughtMonitorWeek> FindForSequence(long[] ids, string stateName)
        {
            List<DroughtMonitorWeek> weeks = new List<DroughtMonitorWeek>();
            IEnumerable<DateTime> dates = PhotoRepository.FindDmDatesForPhotos(ids);

            foreach (DateTime date in dates)
            {
                weeks.Add(DmRepository.FindBy(DmRepository.GetStateForName(stateName), date).FirstOrDefault());
            }
            return weeks;
        }
    }
}
