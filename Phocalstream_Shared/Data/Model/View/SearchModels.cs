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

        //Months
        public bool January { get; set; }
        public bool February { get; set; }
        public bool March { get; set; }
        public bool April { get; set; }
        public bool May { get; set; }
        public bool June { get; set; }
        public bool July { get; set; }
        public bool August { get; set; }
        public bool September { get; set; }
        public bool October { get; set; }
        public bool November { get; set; }
        public bool December { get; set; }

        //HoursOfDay
        public bool Zero { get; set; }
        public bool One { get; set; }
        public bool Two { get; set; }
        public bool Three { get; set; }
        public bool Four { get; set; }
        public bool Five { get; set; }
        public bool Six { get; set; }
        public bool Seven { get; set; }
        public bool Eight { get; set; }
        public bool Nine { get; set; }
        public bool Ten { get; set; }
        public bool Eleven { get; set; }
        public bool Twelve { get; set; }
        public bool Thirteen { get; set; }
        public bool Fourteen { get; set; }
        public bool Fifteen { get; set; }
        public bool Sixteen { get; set; }
        public bool Seventeen { get; set; }
        public bool Eighteen { get; set; }
        public bool Nineteen { get; set; }
        public bool Twenty { get; set; }
        public bool TwentyOne { get; set; }
        public bool TwentyTwo { get; set; }
        public bool TwentyThree { get; set; }


        public ICollection<string> SiteNames { get; set; }
        public ICollection<string> AvailableTags { get; set; }

        public long BackgroundImageID { get; set; }

        
        public bool IsEmpty()
        {
            bool[] month_array = new bool[]
            {
                January, February, March, April, May, June, July, August, September, October, November, December
            };

            bool[] hour_array = new bool[]
            {
                Zero, One, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Eleven,
                Twelve, Thirteen, Fourteen, Fifteen, Sixteen, Seventeen, Eighteen, Nineteen, Twenty, TwentyOne, TwentyTwo, TwentyThree
            };

            foreach (bool month in month_array)
            {
                if(month)
                {
                    return false;
                }
            }

            foreach (bool hour in hour_array)
            {
                if(hour)
                {
                    return false;
                }
            }

            if(String.IsNullOrWhiteSpace(Sites) && String.IsNullOrWhiteSpace(Dates) && String.IsNullOrWhiteSpace(Dates))
            {
                return false;
            }

            return true;
        }

        public string CreateMonthString()
        {
            string months = null;

            bool[] month_array = new bool[]
            {
                January, February, March, April, May, June, July, August, September, October, November, December
            };

            List<int> month_ints = new List<int>();

            int count = month_array.Count();
            for (int i = 0; i < count; i++)
            {
                if(month_array[i])
                {
                    month_ints.Add((i + 1));
                }
            }

            if (month_ints.Count > 0)
            {
                months = string.Join(",", month_ints.Select(n => n.ToString()).ToArray());
            }

            return months;
        }

        public string CreateHourString()
        {
            string hours = null;

            bool[] hour_array = new bool[]
            {
                Zero, One, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Eleven,
                Twelve, Thirteen, Fourteen, Fifteen, Sixteen, Seventeen, Eighteen, Nineteen, Twenty, TwentyOne, TwentyTwo, TwentyThree
            };

            List<int> hour_ints = new List<int>();

            int count = hour_array.Count();
            for (int i = 0; i < count; i++)
            {
                if (hour_array[i])
                {
                    hour_ints.Add((i));
                }
            }

            if (hour_ints.Count > 0)
            {
                hours = string.Join(",", hour_ints.Select(n => n.ToString()).ToArray());
            }

            return hours;
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

            bool[] month_array = new bool[]
            {
                January, February, March, April, May, June, July, August, September, October, November, December
            };
            List<string> monthNames = new List<string>();
            for(int i = 0; i < month_array.Length; i++)
            {
                if(month_array[i])
                {
                    monthNames.Add(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i + 1));
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

            bool[] hour_array = new bool[]
            {
                Zero, One, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Eleven,
                Twelve, Thirteen, Fourteen, Fifteen, Sixteen, Seventeen, Eighteen, Nineteen, Twenty, TwentyOne, TwentyTwo, TwentyThree
            };
            List<string> hourNames = new List<string>();
            for (int i = 0; i < hour_array.Length; i++)
            {
                if (hour_array[i])
                {
                    if (i < 10)
                    {
                        hourNames.Add("0" + Convert.ToString(i) + "00");
                    }
                    else
                    {
                        hourNames.Add(Convert.ToString(i) + "00");
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
        public int PhotoCount { get; set; }
        public string CollectionUrl { get; set; }
    }

    public class SearchList
    {
        public List<Collection> Collections { get; set; }
        public string SearchPath { get; set; }
    }
}