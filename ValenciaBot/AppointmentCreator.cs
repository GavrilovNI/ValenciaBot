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

        _logger.StartMethod(service, center, beforeDate);

        DateTime? result = null;
        SelectService(service);
        if(TrySelectCenter(center))
        {
            _datePicker!.Open();
            bool found = _datePicker.TryGetFirstAvaliableDay(out DateTime dateTime, DateTime.UtcNow.AddHours(_minTimeZone), beforeDate);
            _datePicker!.Close();

            result = found ? dateTime : null;
        }

        _logger.StopMethod(result);

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

        _logger.StartMethod(service, center, beforeDate, name, surname, documentType, document, phoneNumber, email);

        createdTime = beforeDate;

        try
        {
            SelectService(service);
            if(TrySelectCenter(center) == false)
            {
                _logger.StopMethod(false);
                return false;
            }

            DateTime? firstAvaliableDay = GetFirstAvaliableDate(service, center, beforeDate);
            if(firstAvaliableDay == null)
            {
                _logger.StopMethod(false);
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
            {
                _logger.StopMethod(false);
                return false;
            }

            createdTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, selectedTime.Hour, selectedTime.Minute, 1);


            _logger.StopMethod(true , createdTime);
            return true;
        }
        catch (Exception ex)
        {
            _logger.StopMethod(false, ex);
            _logger.LogError("Exception while creating record: " + ex.Message + " " + ex.StackTrace);
            return false;
        }
        finally
        {
            Close();
        }
    }

    public void Open()
    {
        _logger.StartMethod();

        if(Opened)
        {
            _logger.Log("Browser is already opened. Closing");
            Close();
        }
        _driver = new ChromeDriver()
        {
            Url = "http://www.valencia.es/QSIGE/apps/citaprevia/index.html#!/newAppointment/"
        };
        _driver.Wait(TimeoutForLoading);

        UpdateDatePicker();

        _logger.StopMethod();
    }

    public void Close()
    {
        if(Opened == false)
            return;

        _logger.StartMethod();

        _driver!.Close();
        _driver = null;

        _logger.StopMethod();
    }

    private void UpdateDatePicker()
    {
        _logger.StartMethod();

        IWebElement showCalendarButton = _driver!.FindElement(By.XPath("//*[@id=\"datetimepicker2\"]/div/button"));
        IWebElement dropdown = _driver!.FindElement(By.XPath("//*[@id=\"appointmentForm\"]/div[7]/div/div"));

        _datePicker = new DatePicker(_driver, showCalendarButton, dropdown);

        _logger.StopMethod();
    }

    private void SelectService(string text)
    {
        _logger.StartMethod(text);

        SelectElement serviceSelector = _driver!.GetSelector(By.Id("servicios"));
        serviceSelector.SelectByText(text);

        _logger.StopMethod();
    }

    private bool TrySelectCenter(string text)
    {
        _logger.StartMethod(text);

        SelectElement centerSelector = _driver!.GetSelector(By.Id("centros"));
        centerSelector.SelectByText(text);

        var result = TryGetInfoDialog(out Dialog? dialog) == false;
        _logger.StopMethod(result);
        return result;
    }

    private DateTime SelectTime(int index)
    {
        _logger.StartMethod(index);

        SelectElement timeSelector = _driver!.GetSelector(By.Id("hora"));
        timeSelector.SelectByIndex(index);
        var time = timeSelector.Options[index].GetAttribute("label");

        var result = DateTime.ParseExact(time, "HH:mm", CultureInfo.InvariantCulture);
        _logger.StopMethod(result);
        return result;
    }

    private void SetName(string name, string surname)
    {
        _logger.StartMethod(name, surname);

        IWebElement nameElement = _driver!.FindElement(By.Id("nameInput"));
        IWebElement surnameElement = _driver.FindElement(By.Id("surnameInput"));

        nameElement.SendKeys(name);
        surnameElement.SendKeys(surname);

        _logger.StopMethod();
    }

    private void SelectDocumentType(string text)
    {
        _logger.StartMethod(text);

        SelectElement documentTypeSelector = _driver!.GetSelector(By.Id("tipoDocumentos"));
        documentTypeSelector.SelectByText(text);

        _logger.StopMethod();
    }


    private void SetDocument(string document)
    {
        _logger.StartMethod(document);

        IWebElement documentElement = _driver!.FindElement(By.Id("nifInput"));
        documentElement.SendKeys(document);

        _logger.StopMethod();
    }

    private void SetPhoneNumber(string phoneNumber)
    {
        _logger.StartMethod(phoneNumber);

        IWebElement phoneNumberElement = _driver!.FindElement(By.Id("tlfnoInput"));
        phoneNumberElement.SendKeys(phoneNumber);

        _logger.StopMethod();
    }

    private void SetEmail(string email)
    {
        _logger.StartMethod(email);

        IWebElement emailElement = _driver!.FindElement(By.Id("emailInput"));
        emailElement.SendKeys(email);

        _logger.StopMethod();
    }

    private bool TrySubmit()
    {
        _logger.StartMethod();

        IWebElement submitButton = _driver!.FindElement(By.XPath("//*[@id=\"appointmentForm\"]/div[20]/div/button[1]"));
        submitButton.Submit();

        var result = TryGetInfoDialog(out Dialog? dialog);
        _logger.StopMethod(result);
        return result;
    }
}