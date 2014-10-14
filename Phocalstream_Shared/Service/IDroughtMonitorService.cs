using Phocalstream_Shared.Data.Model.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Service
{
    public interface IDroughtMonitorService
    {
        ICollection<DroughtMonitorWeek> FindForSequence(long[] ids, int countyFips);

        ICollection<DroughtMonitorWeek> FindForSequence(long[] ids, string stateName);
    }
}
