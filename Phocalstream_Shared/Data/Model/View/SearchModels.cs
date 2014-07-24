using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
    }

    public class SearchResults
    {
        public string Query { get; set; }
        public string CollectionUrl { get; set; }
    }

    public class SearchList
    {
        public List<Collection> Collections { get; set; }
        public string SearchPath { get; set; }
    }
}