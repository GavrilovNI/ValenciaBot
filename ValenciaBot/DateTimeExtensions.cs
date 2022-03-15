using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValenciaBot.DateTimeExtensions;

public static class DateTimeExtensions
{
    public static int MonthDifference(this DateTime dateTime, DateTime other)
    {
        int yearDifference = other.Year - dateTime.Year;
        int monthDifference = other.Month - dateTime.Month;
        return yearDifference * 12 + monthDifference;
    }
}
