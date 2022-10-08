using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using ValenciaBot.DateTimeExtensions;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;

public class AppointmentCreator : AppoinmentCreationPage
{
    private const int _minTimeZone = -12; // min world time zone

    public AppointmentCreator(BetterChromeDriver driver) : base(driver)
    {
    }

    public DateOnly? GetFirstAvaliableDate(LocationInfo location,
                                           DateOnly beforeDate)
    {
        _logger.StartMethod(location, beforeDate);
        Reload();

        DateOnly? result = null;
        if(TrySelectlocation(location))
        {
            bool found = TryGetFirstAvailableDate(out DateOnly dateTime, DateTime.UtcNow.AddHours(_minTimeZone).ToDateOnly(), beforeDate);
            result = found ? dateTime : null;
        }
        _logger.StopMethod(result!);

        return result;
    }


    public bool CreateAppointmentByExactDate(AppointmentInfo info,
                                             DateOnly exactDate,
                                             out DateTime appointmentDateTime)
    {
        _logger.StartMethod(info, exactDate);

        Reload();

        if(TrySetLocationAndDateTime(info.Location, exactDate, out appointmentDateTime))
        {
            FillPersonInfo(info.PersonInfo);

            var result = TrySubmit();
            if(result)
                Reload();
            _logger.StopMethod(result, appointmentDateTime);
            return result;
        }
        _logger.StopMethod(false, appointmentDateTime);
        return false;
    }

    public bool CreateAppointment(AppointmentInfo info,
                                  DateOnly beforeDate,
                                  out DateTime appointmentDateTime)
    {
        _logger.StartMethod(info, beforeDate);
        Reload();
        appointmentDateTime = beforeDate.ToDateTime();

        try
        {
            DateOnly? firstAvaliableDay = GetFirstAvaliableDate(info.Location, beforeDate);
            if(firstAvaliableDay == null)
            {
                _logger.StopMethod(false);
                return false;
            }
            else
            {
                var result = TrySetLocationAndDateTime(info.Location, firstAvaliableDay.Value, out appointmentDateTime);
                if(result)
                {
                    FillPersonInfo(info.PersonInfo);
                    result = TrySubmit();
                }
                _logger.StopMethod(result);
                return result;
            }
        }
        catch (Exception ex)
        {
            Close();
            _logger.LogError("Exception while creating record: " + ex.Message + " " + ex.StackTrace);
            _logger.StopMethod(false, ex);
            return false;
        }
    }

}