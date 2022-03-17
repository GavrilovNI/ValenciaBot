using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValenciaBot.DateTimeExtensions;

public static class DateTimeExtensions
{
    public static DateOnly ToDateOnly(this DateTime dateTime) =>
        DateOnly.FromDateTime(dateTime);
    public static TimeOnly ToTimeOnly(this DateTime dateTime) =>
        TimeOnly.FromDateTime(dateTime);
    public static DateTime ToDateTime(this DateOnly dateOnly) =>
        dateOnly.ToDateTime(TimeOnly.MinValue);
    public static DateTime ToDateTime(this TimeOnly timeOnly) =>
        DateTime.UnixEpoch.AddTicks(timeOnly.Ticks);
    public static DateTime ToDateTime(this TimeOnly timeOnly, DateOnly dateOnly) =>
        dateOnly.ToDateTime(timeOnly);

    public static int MonthDifference(this DateTime dateTime, DateTime other)
    {
        int yearDifference = other.Year - dateTime.Year;
        int monthDifference = other.Month - dateTime.Month;
        return yearDifference * 12 + monthDifference;
    }

    public static int MonthDifference(this DateOnly dateOnly, DateOnly other) =>
        MonthDifference(dateOnly.ToDateTime(TimeOnly.MinValue), other.ToDateTime(TimeOnly.MinValue));

    public static int MonthDifference(this DateTime dateTime, DateOnly dateOnly) =>
        MonthDifference(dateTime, dateOnly.ToDateTime(TimeOnly.MinValue));

    public static int MonthDifference(this DateOnly dateOnly, DateTime dateTime) =>
        MonthDifference(dateOnly.ToDateTime(TimeOnly.MinValue), dateTime);
}
