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

        public string Group { get; set; }

        public int Index { get; set; }

        public int Limit { get; set; }

        public ICollection<string> SiteNames { get; set; }
        public ICollection<string> AvailableTags { get; set; }
        public UserCollectionList UserCollections { get; set; }

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

            bool found = false;
            if(!String.IsNullOrWhiteSpace(Sites) && !Sites.Equals("undefined"))
            {
                string[] siteSplit = Sites.Split(',');
                if (siteSplit.Length == 1)
                {
                    name.Append("from " + siteSplit[0] + "");
                }
                else if (siteSplit.Length == 2)
                {
                    name.Append("from " + siteSplit[0] + " or " + siteSplit[1] + "");
                }
                else
                {
                    name.Append("from ");
                    for (int i = 0; i < siteSplit.Length - 1; i++)
                    {
                        name.Append(siteSplit[i] + ", ");
                    }
                    name.Append(" or " + siteSplit[siteSplit.Length - 1] + "");
                }
                found = true;
            }

            if (!String.IsNullOrWhiteSpace(Tags) && !Tags.Equals("undefined"))
            {
                if (found)
                {
                    name.Append(", and ");
                }

                string[] tagSplit = Tags.Split(',');
                if (tagSplit.Length == 1)
                {
                    name.Append("tagged with " + tagSplit[0] + "");
                }
                else if (tagSplit.Length == 2)
                {
                    name.Append("tagged with " + tagSplit[0] + " or " + tagSplit[1] + "");
                }
                else
                {
                    name.Append("tagged with ");
                    for (int i = 0; i < tagSplit.Length - 1; i++)
                    {
                        name.Append(tagSplit[i] + ", ");
                    }
                    name.Append(" or " + tagSplit[tagSplit.Length - 1] + "");
                }
                found = true;
            }

            //MONTHS
            List<string> monthNames = new List<string>();
            if (!String.IsNullOrWhiteSpace(this.Months))
            {
                if (found)
                {
                    name.Append(", and ");
                }

                string[] months = this.Months.Split(',');
                foreach (var m in months)
                {
                    monthNames.Add(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Convert.ToInt16(m)));
                }

                if (monthNames.Count == 1)
                {
                    name.Append("taken during the month of " + monthNames[0] + "");
                }
                else if (monthNames.Count == 2)
                {
                    name.Append("taken during the months of " + monthNames[0] + " or " + monthNames[1] + "");
                }
                else
                {
                    name.Append("taken during the months of ");
                    for (int i = 0; i < monthNames.Count - 1; i++)
                    {
                        name.Append(monthNames[i] + ", ");
                    }
                    name.Append(" or " + monthNames[monthNames.Count - 1] + "");
                }
                found = true;
            }


            if (!String.IsNullOrWhiteSpace(Dates) && !Dates.Equals("undefined"))
            {
                if (found)
                {
                    name.Append(", and ");
                }

                string[] dateSplit = Dates.Split(',');
                if (dateSplit.Length == 1)
                {
                    name.Append("taken on " + dateSplit[0] + "");
                }
                else if (dateSplit.Length == 2)
                {
                    name.Append("taken on " + dateSplit[0] + " or " + dateSplit[1] + "");
                }
                else
                {
                    name.Append("taken on ");
                    for (int i = 0; i < dateSplit.Length - 1; i++)
                    {
                        name.Append(dateSplit[i] + ", ");
                    }
                    name.Append(" or " + dateSplit[dateSplit.Length - 1] + "");
                }
                found = true;
            }


            //HOURS
            List<string> hourNames = new List<string>();
            if (!String.IsNullOrWhiteSpace(this.Hours))
            {
                if (found)
                {
                    name.Append(", and ");
                }
             
                string[] hours = this.Hours.Split(',');
                foreach (var h in hours)
                {
                    hourNames.Add(h + "00");
                }

                if (hourNames.Count == 1)
                {
                    name.Append("taken during the hour of " + hourNames[0] + "");
                }
                else if (hourNames.Count == 2)
                {
                    name.Append("taken during the hours of " + hourNames[0] + " or " + hourNames[1] + "");
                }
                else
                {
                    name.Append("taken during the hours of ");
                    for (int i = 0; i < hourNames.Count - 1; i++)
                    {
                        name.Append(hourNames[i] + ", ");
                    }
                    name.Append(" or " + hourNames[hourNames.Count - 1] + "");
                }
                found = true;
            }

            return name.ToString();
        }
    }

    public class SearchMatches
    {
        public List<Photo.Photo> Matches { get; set; }
        public List<long> Ids { get; set; }
    }

}