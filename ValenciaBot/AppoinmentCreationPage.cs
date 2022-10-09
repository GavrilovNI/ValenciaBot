using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using ValenciaBot.DateTimeExtensions;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;
public class AppoinmentCreationPage : DriverWithDialogs<BetterChromeDriver>, ITab
{
    public static readonly string _url = "http://www.valencia.es/QSIGE/apps/citaprevia/index.html#!/newAppointment/";
    private static readonly By _datePickerBy = By.XPath("//*[@id=\"appointmentForm\"]/div[7]/div/div");
    private static readonly By _serviceBy = By.Id("servicios");
    private static readonly By _centerBy = By.Id("centros");
    private static readonly By _timeBy = By.Id("hora");
    private static readonly By _nameBy = By.Id("nameInput");
    private static readonly By _surnameBy = By.Id("surnameInput");
    private static readonly By _documentTypeBy = By.Id("tipoDocumentos");
    private static readonly By _documentBy = By.Id("nifInput");
    private static readonly By _phoneNumberBy = By.Id("tlfnoInput");
    private static readonly By _emailBy = By.Id("emailInput");

    private string _currentTab = String.Empty;

    private UpdatableSelector _serviceSelector;
    private UpdatableSelector _centerSelector;
    private UpdatableSelector _timeSelector;

    private IWebElement? _datePickerElement;

    private DatePicker? _datePicker;
    private UpdatableTextField _nameField;
    private UpdatableTextField _surnameField;
    private UpdatableSelector _documentTypeSelector;
    private UpdatableTextField _documentField;
    private UpdatableTextField _phoneNumberField;
    private UpdatableTextField _emailField;

    private IWebElement? _submitButton;

    public bool Opened => TabExists && _currentTab == _driver.CurrentTab;
    public bool TabExists => _driver.TabExists(_currentTab);

    protected string Service
    {
        get => _serviceSelector.GetSelectorValueText();
        set
        {
            _serviceSelector.SetSelectorValueByText(value);
            WaitLoading(out Dialog _);
        }
    }

    protected string Center
    {
        get => _centerSelector.GetSelectorValueText();
        set
        {
            _centerSelector.SetSelectorValueByText(value);
            WaitLoading(out Dialog _);
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

    private int TimeIndex
    {
        get => _timeSelector.GetSelectorValueIndex();
        set
        {
            _timeSelector.SetSelectorValueByIndex(value);
            WaitLoading(out Dialog _);
        }
    }

    public bool TrySelectFirstAvailableTime(out TimeOnly timeOnly)
    {
        _logger.StartMethod();
        
        timeOnly = new();
        bool result = _timeSelector.SetFirstNotEmptyValue(out string timeStr);
        if(result)
        {
            WaitLoading(out Dialog _);
            result = TryGetInfoDialog(out Dialog? dialog) == false;
            if(result)
                timeOnly = TimeOnly.ParseExact(timeStr, "HH:mm", CultureInfo.InvariantCulture);
            else
                dialog!.Close();
        }
        _logger.StopMethod(result, timeOnly);
        return result;
    }

    public bool TrySelectTime(int index, out TimeOnly timeOnly)
    {
        _logger.StartMethod(index);

        var timeSelector = _timeSelector.SelectElement!;
        var options = timeSelector.Options;
        timeOnly = new();
        bool result = false;
        _logger.Log($"options.Count = {options.Count}");
        _logger.Log($"index = {index}");
        if(index >= 0 && index < options.Count)
        {
            TimeIndex = index;
            result = TryGetInfoDialog(out Dialog? dialog) == false;
            if(result)
            {
                var timeStr = timeSelector.Options[index].GetAttribute("label");
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

    public bool TryGetFirstAvailableDate(out DateOnly dateTime, DateOnly from, DateOnly before)
    {
        var datePicker = DatePicker;
        if(datePicker == null)
            return false;
        datePicker.Open();
        bool found = datePicker.TryGetFirstAvaliableDay(out dateTime, from, before);
        datePicker.Close();
        return found;
    }

    public bool TrySelectlocation(LocationInfo info)
    {
        _logger.StartMethod(info);

        Service = info.Service;
        Center = "";
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
            result = TrySelectFirstAvailableTime(out TimeOnly selectedTime);
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

    private string SurName
    {
        get => _surnameField.GetTextFieldValue();
        set => _surnameField.SetTextFieldValue(value);
    }

    private string Name
    {
        get => _nameField.GetTextFieldValue();
        set => _nameField.SetTextFieldValue(value);
    }

    private string DocumentType
    {
        get => _documentTypeSelector.GetSelectorValueText();
        set => _documentTypeSelector.SetSelectorValueByText(value);
    }

    private string Document
    {
        get => _documentField.GetTextFieldValue();
        set => _documentField.SetTextFieldValue(value);
    }

    private string PhoneNumber
    {
        get => _phoneNumberField.GetTextFieldValue();
        set => _phoneNumberField.SetTextFieldValue(value);
    }

    private string Email
    {
        get => _emailField.GetTextFieldValue();
        set => _emailField.SetTextFieldValue(value);
    }

    public AppoinmentCreationPage(BetterChromeDriver driver) : base(driver)
    {
        _serviceSelector = new(driver, this, _serviceBy);
        _centerSelector = new(driver, this, _centerBy);
        _timeSelector = new(driver, this, _timeBy);
        _nameField = new(driver, this, _nameBy);
        _surnameField = new(driver, this, _surnameBy);
        _documentTypeSelector = new(driver, this, _documentTypeBy);
        _documentField = new(driver, this, _documentBy);
        _phoneNumberField = new(driver, this, _phoneNumberBy);
        _emailField = new(driver, this, _emailBy);
    }

    private void UpdateElements()
    {
        _logger.StartMethod();

        _serviceSelector.Update();
        _centerSelector.Update();
        if(_datePicker == null)
            _datePicker = new DatePicker(_driver, this, _datePickerBy);
        else
            _datePicker.Update(_datePickerBy);
        _timeSelector.Update();
        _nameField.Update();
        _surnameField.Update();
        _documentTypeSelector.Update();
        _documentField.Update();
        _phoneNumberField.Update();
        _emailField.Update();

        _submitButton = _driver!.FindElement(By.XPath("//*[@id=\"appointmentForm\"]/div[20]/div/button[1]"));

        _logger.StopMethod();
    }


    public void Open()
    {
        _logger.StartMethod();

        if(TabExists == false)
            _currentTab = _driver.CreateNewWindow();

        if(TabExists)
            _driver.SetTab(_currentTab);
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

        _serviceSelector.Update();
        SelectElement serviceSelector = _serviceSelector.SelectElement;
        bool loadedWrong = serviceSelector == null;
        if(loadedWrong == false && serviceSelector.Options.Count <= 1)
        {
            var x = serviceSelector.Options;
            var c = serviceSelector.Options.Count;
            loadedWrong = true;
        }

        if(loadedWrong)
        {
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
