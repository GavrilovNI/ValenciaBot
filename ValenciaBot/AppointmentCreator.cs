﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using ValenciaBot.DateTimeExtensions;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;

public class AppointmentCreator : DriverWithDialogs<BetterChromeDriver>, ITab
{
    private readonly string _url = "http://www.valencia.es/QSIGE/apps/citaprevia/index.html#!/newAppointment/";

    private string _currentTab = String.Empty;
    public bool Opened => TabExists && _currentTab == _driver.CurrentTab;
    private bool TabExists => _driver.TabExists(_currentTab);

    private const int _minTimeZone = -12; // min world time zone

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
        _datePicker!.Open();
        var result = _datePicker!.IsDayAvaliable(exactDate);
        if(result)
        {
            _datePicker!.PickDay(exactDate);
            TimeOnly selectedTime = SelectTime(1); // 0 - none; 1,2,3... avaliable times
            appointmentDateTime = exactDate.ToDateTime(selectedTime);
        }
        else
        {
            appointmentDateTime = DateTime.UnixEpoch;
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

        _serviceSelector = _driver.FindSelector(By.Id("servicios"));
        _centerSelector = _driver.FindSelector(By.Id("centros"));
        By datePickerBy = By.XPath("//*[@id=\"appointmentForm\"]/div[7]/div/div");
        if(_datePicker == null)
            _datePicker = new DatePicker(_driver, this, datePickerBy);
        else
            _datePicker.Update(datePickerBy);
        _timeSelector = _driver.FindSelector(By.Id("hora"));
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
        get => GetSelectorValueText(_serviceSelector!);
        set
        {
            SetSelectorValueByText(_serviceSelector!, value);
            WaitLoading(out Dialog _);
        }
    }

    private string Center
    {
        get => GetSelectorValueText(_centerSelector!);
        set
        {
            SetSelectorValueByText(_centerSelector!, value);
            WaitLoading(out Dialog _);
        }
    }

    private int TimeIndex
    {
        get => GetSelectorValueIndex(_timeSelector!);
        set
        {
            SetSelectorValueByIndex(_timeSelector!, value);
            WaitLoading(out Dialog _);
        }
    }

    private TimeOnly SelectTime(int index)
    {
        _logger.StartMethod(index);

        TimeIndex = index;
        var time = _timeSelector!.Options[index].GetAttribute("label");
        
        var result = TimeOnly.ParseExact(time, "HH:mm", CultureInfo.InvariantCulture);
        _logger.StopMethod(result);
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