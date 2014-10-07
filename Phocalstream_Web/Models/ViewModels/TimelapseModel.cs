using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class TimelapseModel
    {
        public TimelapseVideo Video { get; set; }
        public ICollection<long> Ids { get; set; }

        public int FramesPerSecond { get; set; }
        public int BufferCount { get; set; }
    }
}