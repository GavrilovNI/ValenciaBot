

namespace ValenciaBot;

public class AppointmentPreparator : AppoinmentCreationPage
{
    private AppointmentInfo _appointmentInfo;

    public AppointmentPreparator(AppointmentInfo appointmentInfo, BetterChromeDriver driver) : base(driver)
    {
        _appointmentInfo = appointmentInfo;
    }


    public bool Prepare()
    {
        try
        {
            Reload();
            Service = _appointmentInfo.Location.Service;
            FillPersonInfo(_appointmentInfo.PersonInfo);

            return true;
        }
        catch(Exception ex)
        {
            Close();
            _logger.LogError("Exception while preparing: " + ex.Message + " " + ex.StackTrace);
            _logger.StopMethod(false, ex);
            return false;
        }
    }

    public bool Complete(out DateTime dateTime, DateOnly from, DateOnly before, bool autoPrepare = true)
    {
        try
        {
            Center = _appointmentInfo.Location.Center;

            bool found = TryGetFirstAvailableDate(out DateOnly dateOnly, from, before);
            if(found)
            {
                bool timeSelected = TrySelectDateTime(dateOnly, out dateTime);
                var result = TrySubmit();
                return result;
            }
            dateTime = before.ToDateTime(TimeOnly.MinValue);
        }
        catch(Exception ex)
        {
            Close();
            _logger.LogError("Exception while preparing: " + ex.Message + " " + ex.StackTrace);
            _logger.StopMethod(false, ex);
            dateTime = before.ToDateTime(TimeOnly.MinValue);
        }
        if(autoPrepare)
            Prepare();
        return false;
    }

}
