using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Models
{
    public enum DMDataType
    {
        COUNTY,
        STATE,
        US
    }

    public class DroughtMonitorWeek
    {
        public DateTime Week { get; set; }
        public DMDataType Type { get; set; }
        public float NonDrought { get; set; }
        public float D0 { get; set; }
        public float D1 { get; set; }
        public float D2 { get; set; }
        public float D3 { get; set; }
        public float D4 { get; set; }

        public static DateTime ConvertDateToTuesday(DateTime date)
        {
            TimeSpan span = DateTime.Now - date;
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    return date.AddDays(-5);
                case DayOfWeek.Monday:
                    return date.AddDays(-6);
                case DayOfWeek.Tuesday:
                    if (span.Days > 7)
                    {
                        return date;
                    }
                    else  // If current week, then go back to the previous Tuesday
                    {
                        return date.AddDays(-7);
                    }
                case DayOfWeek.Wednesday:
                    if (span.Days > 7)
                    {
                        return date.AddDays(-1);
                    }
                    else // If current week, then go back to the previous Tuesday
                    {
                        return date.AddDays(-8);
                    }
                case DayOfWeek.Thursday:
                    return date.AddDays(-2);
                case DayOfWeek.Friday:
                    return date.AddDays(-3);
                case DayOfWeek.Saturday:
                    return date.AddDays(-4);
                default:
                    return date;
            } //End Switch on Day of Week
        } //End ConvertDateToTuesday

    }
}
