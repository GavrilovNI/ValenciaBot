using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using ValenciaBot.DateTimeExtensions;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;

public class AppointmentCreator : AppoinmentCreationPage
{
    public AppointmentCreator(BetterChromeDriver driver) : base(driver)
    {
    }

    public DateOnly? GetFirstAvaliableDate(LocationInfo location, DateOnly fromDate, DateOnly beforeDate)
    {
        _logger.StartMethod(location, fromDate, beforeDate);
        //Reload();

        DateOnly? result = null;
        if(TrySelectlocation(location))
        {
            bool found = TryGetFirstAvailableDate(out DateOnly dateTime, fromDate, beforeDate);
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

    public bool CreateAppointment(AppointmentInfo info, DateOnly fromDate, DateOnly beforeDate, out DateTime appointmentDateTime)
    {
        _logger.StartMethod(info, beforeDate);
        Reload();
        appointmentDateTime = beforeDate.ToDateTime();

        try
        {
            DateOnly? firstAvaliableDay = GetFirstAvaliableDate(info.Location, fromDate, beforeDate);
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