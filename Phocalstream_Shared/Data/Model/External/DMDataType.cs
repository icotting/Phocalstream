using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data.Model.External
{
    public enum DMDataType
    {
        [Description("County")]
        COUNTY,
        [Description("State")]
        STATE,
        [Description("US")]
        US, 
        [Description("All")]
        ALL
    }
}
