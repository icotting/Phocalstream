using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data.Model.View
{
    public class UserDefinedCollection
    {
        public string CollectionName { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,#}")]
        public int PhotoCount { get; set; }
        public string CollectionUrl { get; set; }
    }

    public class UserCollectionList
    {
        public User User { get; set; }
        public IEnumerable<Collection> Collections { get; set; }
    }

}
