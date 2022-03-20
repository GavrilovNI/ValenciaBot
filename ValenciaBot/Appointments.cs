using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;

public class Appointments : DriverWithDialogs<BetterChromeDriver>, ITab
{
    private string _settedDocument = "";
    public string CurrentDocument { get; private set; } = "";

    private const string _url = "http://www.valencia.es/QSIGE/apps/citaprevia/index.html#!/queryAppoinment";
    
    private string _currentTab = String.Empty;
    public bool Opened => TabExists && _currentTab == _driver.CurrentTab;
    private bool TabExists => _driver.TabExists(_currentTab);

    private IWebElement? _documentField;
    private IWebElement? _submitButton;

    public Appointments(BetterChromeDriver driver) : base(driver)
    {
        
    }

    private void UpdateElements()
    {
        _logger.StartMethod();

        _documentField = _driver.FindElement(By.Id("nif"));
        _submitButton = _driver!.FindElement(By.XPath("html/body/div[2]/div/div[3]/div/div/div[1]/div[2]/form/div[3]/button"));

        _logger.StopMethod();
    }


    public void Reload()
    {
        if(TabExists == false)
            _currentTab = _driver.CreateTab();

        _driver.SetTab(_currentTab);
        _driver.Navigate().GoToUrl(_url);

        IWebElement phoneElementParent2 = _driver.GetElementParent(_driver.FindElement(By.Id("txtTelefono")), 2)!;
        if(phoneElementParent2.GetAttribute("className") != "form-group ng-hide")
        {
            _logger.LogError($"{nameof(Appointments)} page loaded wrong. Reopening");
            Reload();
        }
        else
        {
            UpdateElements();
        }
    }

    public void Open()
    {
        _logger.StartMethod();

        if(TabExists == false)
            _currentTab = _driver.CreateTab();

        _driver.SetTab(_currentTab);
        if(_driver.Url != _url)
            Reload();

        _logger.StopMethod();
    }

    public void Close()
    {
        _logger.StartMethod();

        if(TabExists)
            _driver.CloseTab(_currentTab);
        _currentTab = String.Empty;

        _settedDocument = String.Empty;
        CurrentDocument = String.Empty;

        _logger.StopMethod();
    }

    private Appointment? GetAppointment(string document, Predicate<Appointment> match)
    {
        _logger.StartMethod(document, match);
        Appointment[] appointments = GetAllApointments(document);
        int index = Array.FindIndex(appointments, match);
        var result = index < 0 ? null : appointments[index];
        _logger.StopMethod(result);
        return result;
    }

    public Appointment? GetAppointment(string document, LocationInfo location)
    {
        _logger.StartMethod(document, location);
        var result = GetAppointment(document, a => a.Location == location);
        _logger.StopMethod(result);
        return result;
    }

    private int GetAppointmentIndex(string document, Predicate<Appointment> match)
    {
        _logger.StartMethod(document, match);
        Appointment[] appointments = GetAllApointments(document);
        var result = Array.FindIndex(appointments, match);
        _logger.StopMethod(result);
        return result;
    }

    public bool HasAppointment(string document, Predicate<Appointment> match)
    {
        _logger.StartMethod(document, match);
        bool result = GetAppointmentIndex(document, match) >= 0;
        _logger.StopMethod(result);
        return result;
    }

    public bool HasAppointment(string document, LocationInfo location)
    {
        _logger.StartMethod(document, location);
        bool result = HasAppointment(document, a => a.Location == location);
        _logger.StopMethod(result);
        return result;
    }

    public Appointment[] GetAllApointments(string document)
    {
        _logger.StartMethod(document);
        SetupDocument(document);
        List<Appointment> result = new();
        int i = 0;
        while(TryGetAppointment(document, i, out Appointment? appointment, false))
        {
            result.Add(appointment!);
            i++;
        }
        _logger.StopMethod(result);
        return result.ToArray();
    }

    public bool TryRemoveAppointment(string document, Predicate<Appointment> match)
    {
        _logger.StartMethod(document, match);
        Appointment? appointment = GetAppointment(document, match);
        if(appointment == null)
        {
            _logger.StopMethod(false);
            return false;
        }

        appointment.Remove();
        _logger.StopMethod(true);
        return true;
    }

    public bool TryRemoveAppointment(string document, LocationInfo location)
    {
        _logger.StartMethod(document, location);
        var result = TryRemoveAppointment(document, a => a.Location == location);
        _logger.StopMethod(result);
        return result;
    }

    public bool TryRemoveAppointment(string document, LocationInfo location, DateTime dateTime)
    {
        _logger.StartMethod(document, location, dateTime);
        bool timeMatch(DateTime time) => time.Year == dateTime.Year &&
                                        time.Month == dateTime.Month &&
                                        time.Day == dateTime.Day &&
                                        time.Hour == dateTime.Hour &&
                                        time.Minute == dateTime.Minute;
        var result = TryRemoveAppointment(document, a => a.Location == location && timeMatch(a.DateTime));
        _logger.StopMethod(result);
        return result;
    }

    private void SetupDocument(string document)
    {
        _logger.StartMethod(document);

        Open();

        SetDocument(document);
        SubmitDocument();

        _logger.StopMethod();
    }

    private void SetDocument(string document)
    {
        _logger.StartMethod(document);
        _documentField!.Clear();
        _documentField.SendKeys(document);
        _settedDocument = document;
        _logger.StopMethod();
    }

    private void SubmitDocument()
    {
        _logger.StartMethod();
        _submitButton!.Submit();
        WaitLoading(out Dialog _);
        CurrentDocument = _settedDocument;
        _logger.StopMethod();
    }

    private bool TryGetAppointment(string document, int index, out Appointment? appointment, bool reload = true)
    {
        _logger.StartMethod(index);
        if(reload)
            SetupDocument(document);
        try
        {
            IWebElement appointmentElement = _driver!.FindElement(By.XPath($"/html/body/div[2]/div/div[3]/div/div/div[2]/div[2]/table/tbody/tr[1]/td[{index + 1}]"));
            appointment = new Appointment(_driver, appointmentElement);
            _logger.StopMethod(true, appointment);
            return true;
        }
        catch(NoSuchElementException)
        {
            appointment = null;
            _logger.StopMethod(false);
            return false;
        }
    }

}
