using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using ValenciaBot.DateTimeExtensions;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;

public class AppointmentCreator : DriverWithDialogs<BetterChromeDriver>
{
    private readonly string _url = "http://www.valencia.es/QSIGE/apps/citaprevia/index.html#!/newAppointment/";

    private string _currentTab = String.Empty;
    public bool Opened => TabExists && _currentTab == _driver.CurrentTab;
    private bool TabExists => _driver.TabExists(_currentTab);

    private DatePicker? _datePicker;
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
        SelectService(location.Service);
        if(TrySelectCenter(location.Center))
        {
            _datePicker!.Open();
            bool found = _datePicker.TryGetFirstAvaliableDay(out DateOnly dateTime, DateTime.UtcNow.AddHours(_minTimeZone).ToDateOnly(), beforeDate);
            _datePicker!.Close();

            result = found ? dateTime : null;
        }

        _logger.StopMethod(result!);

        return result;
    }

    public bool CreateAppointmentByExactDate(AppointmentInfo info,
                                             DateOnly exactDate,
                                             out DateTime appointmentDateTime)
    {
        Open();

        _logger.StartMethod(info, exactDate);

        appointmentDateTime = exactDate.ToDateTime();

        try
        {
            SelectService(info.Location.Service);
            if(TrySelectCenter(info.Location.Center) == false)
            {
                _logger.StopMethod(false);
                return false;
            }
            _datePicker!.Open();
            bool dayAvaliable = _datePicker!.IsDayAvaliable(exactDate);
            if(dayAvaliable)
            {
                _datePicker!.PickDay(exactDate);
            }
            else
            {
                _logger.StopMethod(false, "Day is not avaliable");
                return false;
            }

            DateOnly choosenDate = exactDate;
            TimeOnly selectedTime = SelectTime(1); // 0 - none; 1,2,3... avaliable times

            SetName(info.Name, info.Surname);
            SelectDocumentType(info.DocumentType);
            SetDocument(info.Document);
            SetPhoneNumber(info.PhoneNumber);
            SetEmail(info.Email);

            appointmentDateTime = choosenDate.ToDateTime(selectedTime);

            var result = TrySubmit();
            _logger.StopMethod(result, appointmentDateTime);
            return result;
        }
        catch(Exception ex)
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

    public bool CreateAppointment(AppointmentInfo info,
                                  DateOnly beforeDate,
                                  out DateTime appointmentDateTime)
    {
        Open();

        _logger.StartMethod(info, beforeDate);

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
                return CreateAppointmentByExactDate(info, firstAvaliableDay.Value, out appointmentDateTime);
            }
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

        if(TabExists == false )
            _currentTab = _driver.CreateNewWindow();

        if(_driver.Url != _url)
            Reload();
        _logger.StopMethod();
    }

    public void Reload()
    {
        _logger.StartMethod();

        if(TabExists == false)
            _currentTab = _driver.CreateTab();

        _driver.SetTab(_currentTab);

        _driver.Navigate().GoToUrl(_url);
        _driver.Wait(TimeoutForLoading);

        UpdateDatePicker();
        _logger.StopMethod();
    }

    public void Close()
    {
        _logger.StartMethod();

        if(TabExists)
            _driver.CloseTab(_currentTab);
        _currentTab = String.Empty;

        _logger.StopMethod();
    }

    private void UpdateDatePicker()
    {
        _logger.StartMethod();

        IWebElement showCalendarButton = _driver!.FindElement(By.XPath("//*[@id=\"datetimepicker2\"]/div/button"));
        IWebElement dropdown = _driver!.FindElement(By.XPath("//*[@id=\"appointmentForm\"]/div[7]/div/div"));

        _datePicker = new DatePicker(showCalendarButton, dropdown);

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

        var result = TryGetInfoDialog(out Dialog? _) == false;
        _logger.StopMethod(result);
        return result;
    }

    private TimeOnly SelectTime(int index)
    {
        _logger.StartMethod(index);

        SelectElement timeSelector = _driver!.GetSelector(By.Id("hora"));
        timeSelector.SelectByIndex(index);
        var time = timeSelector.Options[index].GetAttribute("label");
        
        var result = TimeOnly.ParseExact(time, "HH:mm", CultureInfo.InvariantCulture);
        _logger.StopMethod(result);
        return result;
    }

    private void SetName(string name, string surname)
    {
        _logger.StartMethod(name, surname);

        IWebElement nameElement = _driver!.FindElement(By.Id("nameInput"));
        IWebElement surnameElement = _driver.FindElement(By.Id("surnameInput"));

        nameElement.Clear();
        surnameElement.Clear();
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
        documentElement.Clear();
        documentElement.SendKeys(document);

        _logger.StopMethod();
    }

    private void SetPhoneNumber(string phoneNumber)
    {
        _logger.StartMethod(phoneNumber);

        IWebElement phoneNumberElement = _driver!.FindElement(By.Id("tlfnoInput"));
        phoneNumberElement.Clear();
        phoneNumberElement.SendKeys(phoneNumber);

        _logger.StopMethod();
    }

    private void SetEmail(string email)
    {
        _logger.StartMethod(email);

        IWebElement emailElement = _driver!.FindElement(By.Id("emailInput"));
        emailElement.Clear();
        emailElement.SendKeys(email);

        _logger.StopMethod();
    }

    private bool TrySubmit()
    {
        _logger.StartMethod();

        IWebElement submitButton = _driver!.FindElement(By.XPath("//*[@id=\"appointmentForm\"]/div[20]/div/button[1]"));
        submitButton.Submit();

        var result = TryGetInfoDialog(out Dialog? _) == false;
        _logger.StopMethod(result);
        return result;
    }
}