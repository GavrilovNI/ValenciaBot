using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using ValenciaBot.DateTimeExtensions;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;

public class AppointmentCreator : DriverWithDialogs<BetterChromeDriver>, ITab
{
    private readonly string _url = "http://www.valencia.es/QSIGE/apps/citaprevia/index.html#!/newAppointment/";
    private static readonly By _datePickerBy = By.XPath("//*[@id=\"appointmentForm\"]/div[7]/div/div");
    private static readonly By _serviceBy = By.Id("servicios");
    private static readonly By _centerBy = By.Id("centros");
    private static readonly By _timeBy = By.Id("hora");

    private string _currentTab = String.Empty;
    public bool Opened => TabExists && _currentTab == _driver.CurrentTab;
    private bool TabExists => _driver.TabExists(_currentTab);

    private const int _minTimeZone = -12; // min world time zone

    private IWebElement? _serviceSelectorElement;
    private IWebElement? _centereSelectorElement;
    private IWebElement? _datePickerElement;
    private IWebElement? _timeSelectorElement;

    private SelectElement? _serviceSelector;
    private SelectElement? _centerSelector;
    private DatePicker? _datePicker;
    private SelectElement? _timeSelector;
    private IWebElement? _nameField;
    private IWebElement? _surnameField;
    private SelectElement? _documentTypeSelector;
    private IWebElement? _documentField;
    private IWebElement? _phoneNumberField;
    private IWebElement? _emailField;

    private IWebElement? _submitButton;



    protected SelectElement? ServiceSelector
    {
        get
        {
            if(_serviceSelector == null)
            {
                if(_serviceSelectorElement == null)
                    _serviceSelectorElement = _driver.FindElement(_serviceBy);
                _serviceSelector = _driver.GetSelector(_serviceSelectorElement);

            }
            return _serviceSelector;
        }
    }
    protected SelectElement? CenterSelector
    {
        get
        {
            if(_centerSelector == null)
            {
                if(_centereSelectorElement == null)
                    _centereSelectorElement = _driver.FindElement(_centerBy);
                _centerSelector = _driver.GetSelector(_centereSelectorElement);

            }
            return _centerSelector;
        }
    }
    protected DatePicker? DatePicker
    {
        get
        {
            if(_datePicker == null)
                _datePicker = new DatePicker(_driver, this, _datePickerBy);
            return _datePicker;
        }
    }
    protected SelectElement? TimeSelector
    {
        get
        {
            if(_timeSelector == null)
            {
                if(_timeSelectorElement == null)
                    _timeSelectorElement = _driver.FindElement(_timeBy);
                _timeSelector = _driver.GetSelector(_timeSelectorElement);

            }
            return _timeSelector;
        }
    }


    public AppointmentCreator(BetterChromeDriver driver) : base(driver)
    {
        
    }

    public void FillPersonInfo(PersonInfo info)
    {
        _logger.StartMethod(info);
        Open();

        Name = info.Name;
        SurName = info.Surname;
        DocumentType = info.DocumentType;
        Document = info.Document;
        PhoneNumber = info.PhoneNumber;
        Email = info.Email;

        _logger.StopMethod();
    }

    public bool TrySelectlocation(LocationInfo info)
    {
        _logger.StartMethod(info);

        Service = info.Service;
        Center = info.Center;
        bool result = TryGetInfoDialog(out Dialog? dialog) == false;
        dialog?.Close();

        _logger.StopMethod(result);
        return result;
    }

    public bool TrySelectDateTime(DateOnly exactDate, out DateTime appointmentDateTime)
    {
        _logger.StartMethod(exactDate);
        appointmentDateTime = DateTime.UnixEpoch;
        _datePicker!.Open();
        var result = _datePicker!.IsDayAvaliable(exactDate);
        if(result)
        {
            _datePicker!.PickDay(exactDate);
            result = TrySelectTime(1, out TimeOnly selectedTime); // 0 - none; 1,2,3... avaliable times
            if(result)
                appointmentDateTime = exactDate.ToDateTime(selectedTime);
        }
        _datePicker!.Close();

        _logger.StopMethod(result, appointmentDateTime);
        return result;
    }

    public bool TrySetLocationAndDateTime(LocationInfo info,
                                          DateOnly exactDate,
                                          out DateTime appointmentDateTime)
    {
        _logger.StartMethod(info, exactDate);

        Open();

        bool result = TrySelectlocation(info);
        if(result)
            result = TrySelectDateTime(exactDate, out appointmentDateTime);
        else
            appointmentDateTime = DateTime.UnixEpoch;

        _logger.StopMethod(result, appointmentDateTime);
        return result;
    }

    public DateOnly? GetFirstAvaliableDate(LocationInfo location,
                                           DateOnly beforeDate)
    {
        _logger.StartMethod(location, beforeDate);
        Reload();

        DateOnly? result = null;
        if(TrySelectlocation(location))
        {
            _datePicker!.Open();
            bool found = _datePicker.TryGetFirstAvaliableDay(out DateOnly dateTime, DateTime.UtcNow.AddHours(_minTimeZone).ToDateOnly(), beforeDate);
            _datePicker.Close();

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

        WaitLoading(out Dialog _);

        SelectElement serviceSelector = _driver!.FindSelector(By.Id("servicios"));
        if(serviceSelector.Options.Count <= 1)
        {
            var x = serviceSelector.Options;
            var c = serviceSelector.Options.Count;
            _logger.LogError($"{nameof(AppointmentCreator)} page loaded wrong. Reopening");
            Reload();
        }
        else
        {
            UpdateElements();
        }

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

    private void UpdateElements()
    {
        _logger.StartMethod();

        _serviceSelectorElement = _driver.FindElement(_serviceBy);
        _serviceSelector = _driver.GetSelector(_serviceSelectorElement);
        _centereSelectorElement = _driver.FindElement(_centerBy);
        _centerSelector = _driver.GetSelector(_centereSelectorElement);
        if(_datePicker == null)
            _datePicker = new DatePicker(_driver, this, _datePickerBy);
        else
            _datePicker.Update(_datePickerBy);
        _timeSelectorElement = _driver.FindElement(_timeBy);
        _timeSelector = _driver.GetSelector(_timeSelectorElement);
        _nameField = _driver.FindElement(By.Id("nameInput"));
        _surnameField = _driver.FindElement(By.Id("surnameInput"));
        _documentTypeSelector = _driver.FindSelector(By.Id("tipoDocumentos"));
        _documentField = _driver.FindElement(By.Id("nifInput"));
        _phoneNumberField = _driver.FindElement(By.Id("tlfnoInput"));
        _emailField = _driver.FindElement(By.Id("emailInput"));

        _submitButton = _driver!.FindElement(By.XPath("//*[@id=\"appointmentForm\"]/div[20]/div/button[1]"));

        _logger.StopMethod();
    }

    private void SetSelectorValueByIndex(SelectElement selector, int index)
    {
        _logger.StartMethod(selector, index);
        selector.SelectByIndex(index);
        _logger.StopMethod();
    }

    private int GetSelectorValueIndex(SelectElement selector)
    {
        _logger.StartMethod(selector);

        var result = -1;
        for(int i = 0; i < selector.Options.Count; i++)
        {
            if(_driver.ElementsEqual(selector.Options[i], selector.SelectedOption))
            {
                result = i;
                break;
            }
        }

        _logger.StopMethod(result);
        return result;
    }

    private void SetSelectorValueByText(SelectElement selector, string value)
    {
        _logger.StartMethod(selector, value);
        selector.SelectByText(value);
        _logger.StopMethod();
    }

    private string GetSelectorValueText(SelectElement selector)
    {
        _logger.StartMethod(selector);
        var result = selector.SelectedOption.GetAttribute("innerHTML");
        _logger.StopMethod(result);
        return result;
    }

    private void SetTextFieldValue(IWebElement inputField, string value)
    {
        _logger.StartMethod(inputField, value);
        inputField.Clear();
        inputField.SendKeys(value);
        _logger.StopMethod();
    }

    private string GetTextFieldValue(IWebElement inputField)
    {
        _logger.StartMethod(inputField);
        var result = inputField.GetAttribute("value");
        _logger.StopMethod(result);
        return result;
    }

    private string Service
    {
        get => GetSelectorValueText(ServiceSelector!);
        set
        {
            SetSelectorValueByText(ServiceSelector!, value);
            WaitLoading(out Dialog _);
        }
    }

    private string Center
    {
        get => GetSelectorValueText(CenterSelector!);
        set
        {
            SetSelectorValueByText(CenterSelector!, value);
            WaitLoading(out Dialog _);
        }
    }

    private int TimeIndex
    {
        get => GetSelectorValueIndex(TimeSelector!);
        set
        {
            SetSelectorValueByIndex(TimeSelector!, value);
            WaitLoading(out Dialog _);
        }
    }

    private bool TrySelectTime(int index, out TimeOnly timeOnly)
    {
        _logger.StartMethod(index);

        var options = TimeSelector!.Options;
        timeOnly = new();
        bool result = false;
        if(index >= 0 && index < options.Count)
        {
            TimeIndex = index;
            result = TryGetInfoDialog(out Dialog? dialog) == false;
            if(result)
            {
                var timeStr = TimeSelector!.Options[index].GetAttribute("label");
                timeOnly = TimeOnly.ParseExact(timeStr, "HH:mm", CultureInfo.InvariantCulture);
            }
            else
            {
                dialog!.Close();
            }
        }
        _logger.StopMethod(result, timeOnly);
        return result;
    }


    private string SurName
    {
        get => GetTextFieldValue(_surnameField!);
        set => SetTextFieldValue(_surnameField!, value);
    }

    private string Name
    {
        get => GetTextFieldValue(_nameField!);
        set => SetTextFieldValue(_nameField!, value);
    }

    private string DocumentType
    {
        get => GetSelectorValueText(_documentTypeSelector!);
        set => SetSelectorValueByText(_documentTypeSelector!, value);
    }

    private string Document
    {
        get => GetTextFieldValue(_documentField!);
        set => SetTextFieldValue(_documentField!, value);
    }

    private string PhoneNumber
    {
        get => GetTextFieldValue(_phoneNumberField!);
        set => SetTextFieldValue(_phoneNumberField!, value);
    }

    private string Email
    {
        get => GetTextFieldValue(_emailField!);
        set => SetTextFieldValue(_emailField!, value);
    }

    public bool TrySubmit()
    {
        _logger.StartMethod();

        if(Opened == false)
            return false;

        _submitButton!.Submit();

        var dialogFound = TryGetInfoDialog(out Dialog? dialog);
        if(dialogFound)
            dialog!.Close();
        //wait for page loading here if you dont use TryGetInfoDialog
        var result = dialogFound == false && _driver.Url != _url;
        _logger.StopMethod(result);
        return result;
    }
}