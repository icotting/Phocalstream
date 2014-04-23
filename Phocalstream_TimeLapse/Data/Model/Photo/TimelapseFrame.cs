using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Phocalstream_Shared.Data.Model.Photo
{
    [XmlRoot("Frame")]
    public class TimelapseFrame
    {
        [XmlAttribute]
        public DateTime Time { get; set; }

        [XmlAttribute]
        public string Url { get; set; }

        [XmlAttribute]
        public long PhotoId { get; set; }
    }

    [XmlRoot("TimelapseVideo")]
    public class TimelapseVideo
    {
        public List<TimelapseFrame> Frames { get; set; }
    }
}