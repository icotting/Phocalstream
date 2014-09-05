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

            if(!String.IsNullOrWhiteSpace(Sites))
            {

                name.Append("from " + String.Join(", ", Sites.Split(',')) + " ");
            }

            if (!String.IsNullOrWhiteSpace(Tags))
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

            if (!String.IsNullOrWhiteSpace(Dates))
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
                    var hInt = Convert.ToInt16(h);
                    if (hInt < 10)
                    {
                        hourNames.Add("0" + h + "00");
                    }
                    else
                    {
                        hourNames.Add(h + "00");
                    }
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
        
        [DisplayFormat(DataFormatString = "{0:#,#}")]
        public int PhotoCount { get; set; }
        public string CollectionUrl { get; set; }
    }

    public class SearchList
    {
        public List<Collection> Collections { get; set; }
        public string SearchPath { get; set; }
    }

    public class QuickSearchModel
    {
        public string Sites { get; set; }
        public string Tags { get; set; }
        public string Dates { get; set; }
        public string Hours { get; set; }
        public string Months { get; set; }
        public bool IsEmpty()
        {
            return String.IsNullOrWhiteSpace(Sites) 
                && String.IsNullOrWhiteSpace(Tags)
                && String.IsNullOrWhiteSpace(Dates) 
                && String.IsNullOrWhiteSpace(Hours)
                && String.IsNullOrWhiteSpace(Months);
        }
    }
}