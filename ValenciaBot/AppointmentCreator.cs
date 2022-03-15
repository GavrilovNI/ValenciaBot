using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;

public class AppointmentCreator : DriverWithDialogs
{
    public bool Opened => _driver != null;

    private DatePicker? _datePicker;

    private const int _minTimeZone = -12; // min world time zone


    public DateTime? GetFirstAvaliableDate(string service,
                                           string center,
                                           DateTime beforeDate)
    {
        if(Opened == false)
            throw new InvalidOperationException(nameof(AppointmentCreator) + " is not opened.");

        Logger.Log($"Getting First Avaliable Date. BeforeDate:{beforeDate:dd:MM:yyyy} service: '{service}' center: '{center}'");

        SelectService(service);
        if(TrySelectCenter(center) == false)
            return null;

        _datePicker!.Open();
        bool found = _datePicker.TryGetFirstAvaliableDay(out DateTime dateTime, DateTime.UtcNow.AddHours(_minTimeZone), beforeDate);
        _datePicker!.Close();

        DateTime? result = found ? dateTime : null;
        Logger.Log((found ?  $"Got First Avaliable Date. Result: {result}" : "Not found avaliable date") + $" BeforeDate:{beforeDate:dd:MM:yyyy} service: '{service}' center: '{center}'");

        return result;
    }

    public bool CreateAppointment(string service,
                                  string center,
                                  DateTime beforeDate,
                                  string name,
                                  string surname,
                                  string documentType,
                                  string document,
                                  string phoneNumber,
                                  string email,
                                  out DateTime createdTime)
    {
        if(Opened == false)
            throw new InvalidOperationException(nameof(AppointmentCreator) + " is not opened.");

        Logger.Log($"Appointment Creating. BeforeDate:{beforeDate:dd:MM:yyyy} service: '{service}' center: '{center}'");
        
        createdTime = beforeDate;

        try
        {
            SelectService(service);
            if(TrySelectCenter(center) == false)
                return false;

            DateTime? firstAvaliableDay = GetFirstAvaliableDate(service, center, beforeDate);
            if(firstAvaliableDay == null)
            {
                Logger.Log($"CreateAppointment: No avaliable day found.");
                return false;
            }
            else
            {
                _datePicker!.Open();
                _datePicker!.PickDay(firstAvaliableDay.Value.Day);
                _datePicker!.Close();
            }
            DateTime dateTime = firstAvaliableDay.Value;

            DateTime selectedTime = SelectTime(1); // 0 - none; 1,2,3... avaliable times

            SetName(name, surname);
            SelectDocumentType(documentType);
            SetDocument(document);
            SetPhoneNumber(phoneNumber);
            SetEmail(email);

            if(TrySubmit() == false)
                return false;

            createdTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, selectedTime.Hour, selectedTime.Minute, 1);

            Logger.Log($"Appointment Created! Time:{createdTime:hh:mm dd:MM:yyyy} service: '{service}' center: '{center}'");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Exception while creating record: " + ex.Message + " " + ex.StackTrace);
            return false;
        }
        finally
        {
            Close();
        }
    }

    public void Open()
    {
        if(Opened)
            Close();
        _driver = new ChromeDriver
        {
            Url = "http://www.valencia.es/QSIGE/apps/citaprevia/index.html#!/newAppointment/"
        };
        _driver.Wait(TimeoutForLoading);

        UpdateDatePicker();
    }

    public void Close()
    {
        if(Opened == false)
            return;
        _driver!.Close();
        _driver = null;
        Logger.Log(nameof(AppointmentCreator) + " closed");
    }

    private void UpdateDatePicker()
    {
        IWebElement showCalendarButton = _driver!.FindElement(By.XPath("//*[@id=\"datetimepicker2\"]/div/button"));
        IWebElement dropdown = _driver!.FindElement(By.XPath("//*[@id=\"appointmentForm\"]/div[7]/div/div"));

        _datePicker = new DatePicker(_driver, showCalendarButton, dropdown);
        Logger.Log("Date picker updated");
    }

    private void SelectService(string text)
    {
        Logger.Log($"SelectService '{text}'");
        SelectElement serviceSelector = _driver!.GetSelector(By.Id("servicios"));
        serviceSelector.SelectByText(text);
    }

    private bool TrySelectCenter(string text)
    {
        Logger.Log($"SelectCenter '{text}'");
        SelectElement centerSelector = _driver!.GetSelector(By.Id("centros"));
        centerSelector.SelectByText(text);

        if(TryGetInfoDialog(out Dialog? dialog))
        {
            Logger.Log($"SelectCenter : '{dialog!.Content}'");
            return false;
        }
        return true;
    }

    private DateTime SelectTime(int index)
    {
        Logger.Log($"SelectTime '{index}'");
        SelectElement timeSelector = _driver!.GetSelector(By.Id("hora"));
        timeSelector.SelectByIndex(index);
        var time = timeSelector.Options[index].GetAttribute("label");

        return DateTime.ParseExact(time, "hh:mm", CultureInfo.InvariantCulture);
    }

    private void SetName(string name, string surname)
    {
        Logger.Log($"SetName '{name}' '{surname}'");
        IWebElement nameElement = _driver!.FindElement(By.Id("nameInput"));
        IWebElement surnameElement = _driver.FindElement(By.Id("surnameInput"));

        nameElement.SendKeys(name);
        surnameElement.SendKeys(surname);
    }

    private void SelectDocumentType(string text)
    {
        Logger.Log($"SelectDocumentType '{text}'");
        SelectElement documentTypeSelector = _driver!.GetSelector(By.Id("tipoDocumentos"));
        documentTypeSelector.SelectByText(text);
    }


    private void SetDocument(string document)
    {
        Logger.Log($"SetDocument '{document}'");
        IWebElement documentElement = _driver!.FindElement(By.Id("nifInput"));
        documentElement.SendKeys(document);
    }

    private void SetPhoneNumber(string phoneNumber)
    {
        Logger.Log($"SetPhoneNumber '{phoneNumber}'");
        IWebElement phoneNumberElement = _driver!.FindElement(By.Id("tlfnoInput"));
        phoneNumberElement.SendKeys(phoneNumber);
    }

    private void SetEmail(string email)
    {
        Logger.Log($"SetEmail '{email}'");
        IWebElement emailElement = _driver!.FindElement(By.Id("emailInput"));
        emailElement.SendKeys(email);
    }

    private bool TrySubmit()
    {
        Logger.Log($"Submit");
        IWebElement submitButton = _driver!.FindElement(By.XPath("//*[@id=\"appointmentForm\"]/div[20]/div/button[1]"));
        submitButton.Submit();

        if(TryGetInfoDialog(out Dialog? dialog))
        {
            Logger.LogError($"Submit error '{dialog!.Content}'");
            return false;
        }
        return true;
    }

    

}