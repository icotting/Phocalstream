using Phocalstream_Shared.Data.Model.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data
{
    public interface IDroughtMonitorRepository
    {
        ICollection<DroughtMonitorWeek> Fetch(DMDataType type);

        ICollection<DroughtMonitorWeek> FindBy(USCounty county, DateTime? week = null, int weeksPrevious = 0);
        ICollection<DroughtMonitorWeek> FindBy(USState state, DateTime? week = null, int weeksPrevious = 0);
        ICollection<DroughtMonitorWeek> FindUS(DateTime? week = null, int weeksPrevious = 0);

        USState GetStateForName(string name);
        USState GetState(long id);

        USCounty GetCounty(long id);
        USCounty GetCountyForFips(int fips);

        DateTime GetDmDate(int type);

        void Add(DroughtMonitorWeek week);
    }
}
