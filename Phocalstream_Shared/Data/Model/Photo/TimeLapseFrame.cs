using Phocalstream_Shared.Data.Model.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data.Model.Photo
{
    public class TimeLapseFrame
    {
        public long PhotoID { get; set; }
        public DateTime FrameTime { get; set; }
    }
}
