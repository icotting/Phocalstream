using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

namespace Phocalstream_Shared.Data.Model.View
{
    public class SearchModel
    {
        [Display(Name = "Sites")]
        public string Sites { get; set; }

        [Display(Name = "Dates")]
        public string Dates { get; set; }

        [Display(Name = "Tags")]
        public string Tags { get; set; }

        public string Months { get; set; }
        
        public string Hours { get; set; }

        public ICollection<string> SiteNames { get; set; }
        public ICollection<string> AvailableTags { get; set; }

        public long BackgroundImageID { get; set; }
        
        public bool IsEmpty()
        {
            return String.IsNullOrWhiteSpace(Sites)
                && String.IsNullOrWhiteSpace(Tags)
                && String.IsNullOrWhiteSpace(Dates)
                && String.IsNullOrWhiteSpace(Hours) 
                && String.IsNullOrWhiteSpace(Months);
        }

        public string CreateCollectionName()
        {
            StringBuilder name = new StringBuilder(); 
            name.Append("Showing photos ");

            if(!String.IsNullOrWhiteSpace(Sites) && !Sites.Equals("undefined"))
            {
                string[] siteSplit = Sites.Split(',');
                if (siteSplit.Length == 1)
                {
                    name.Append("from " + siteSplit[0] + " ");
                }
                else if (siteSplit.Length == 2)
                {
                    name.Append("from " + siteSplit[0] + " or " + siteSplit[1] + " ");
                }
                else
                {
                    name.Append("from ");
                    for (int i = 0; i < siteSplit.Length - 1; i++)
                    {
                        name.Append(siteSplit[i] + ", ");
                    }
                    name.Append(" or " + siteSplit[siteSplit.Length - 1] + " ");
                }
            }

            if (!String.IsNullOrWhiteSpace(Tags) && !Tags.Equals("undefined"))
            {
                name.Append("tagged with " + String.Join(", ", Tags.Split(',')) + " ");
            }

            //MONTHS
            List<string> monthNames = new List<string>();
            if (!String.IsNullOrWhiteSpace(this.Months))
            {
                string[] months = this.Months.Split(',');
                foreach (var m in months)
                {
                    monthNames.Add(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Convert.ToInt16(m)));
                }

            }
            if(monthNames.Count != 0)
            {
                name.Append("taken during " + String.Join(", ", monthNames.ToArray()) + " ");
            }

            if (!String.IsNullOrWhiteSpace(Dates) && !Dates.Equals("undefined"))
            {
                name.Append("taken on " + String.Join(", ", Dates.Split(',')) + " ");
            }


            //HOURS
            List<string> hourNames = new List<string>();
            if (!String.IsNullOrWhiteSpace(this.Hours))
            {
                string[] hours = this.Hours.Split(',');
                foreach (var h in hours)
                {
                    hourNames.Add(h + "00");
                }
            }
            if(hourNames.Count != 0)
            {
                name.Append("during the hours of " + String.Join(", ", hourNames.ToArray()) + ".");
            }

            return name.ToString();
        }
    }

    public class SearchMatches
    {
        public List<Photo.Photo> Matches { get; set; }
        public List<long> Ids { get; set; }
    }

    public class SearchResults
    {
        public string CollectionName { get; set; }

        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime First { get; set; }

        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime Last { get; set; }
        
        [DisplayFormat(DataFormatString = "{0:#,#}")]
        public int PhotoCount { get; set; }
        public string CollectionUrl { get; set; }
        public UserCollectionList UserCollections { get; set; }
    }

    public class SearchList
    {
        public List<Collection> Collections { get; set; }
        public string SearchPath { get; set; }
    }
}