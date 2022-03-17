using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;

public class Appointments : DriverWithDialogs
{
    public bool Opened => _driver != null;

    private string _document = "761234567";


    public Appointments(string document)
    {
        _logger.Log($"Start. Document: '{document}'");
        _document = document;
    }

    public void ChangeDocument(string document)
    {
        _document = document;
        if(Opened)
        {
            Close();
            Open();
        }
    }

    private Appointment? GetAppointment(Predicate<Appointment> match)
    {
        _logger.StartMethod(match);
        Appointment[] appointments = GetAllApointments();
        int index = Array.FindIndex(appointments, match);
        var result = index < 0 ? null : appointments[index];
        _logger.StopMethod(result);
        return result;
    }

    public Appointment? GetAppointment(string service, string center)
    {
        _logger.StartMethod(service, center);
        var result = GetAppointment(a => a.Service == service && a.Center == center);
        _logger.StopMethod(result);
        return result;
    }

    private int GetAppointmentIndex(Predicate<Appointment> match)
    {
        _logger.StartMethod(match);
        Appointment[] appointments = GetAllApointments();
        var result = Array.FindIndex(appointments, match);
        _logger.StopMethod(result);
        return result;
    }

    public bool HasAppointment(Predicate<Appointment> match)
    {
        _logger.StartMethod(match);
        bool result = GetAppointmentIndex(match) >= 0;
        _logger.StopMethod(result);
        return result;
    }

    public bool HasAppointment(string service, string center)
    {
        _logger.StartMethod(service, center);
        bool result = HasAppointment(a => a.Service == service && a.Center == center);
        _logger.StopMethod(result);
        return result;
    }

    public Appointment[] GetAllApointments()
    {
        _logger.StartMethod();
        List<Appointment> result = new();
        int i = 0;
        while(TryGetAppointment(i, out Appointment? appointment))
        {
            result.Add(appointment!);
            i++;
        }
        _logger.StopMethod(result);
        return result.ToArray();
    }

    public bool TryRemoveAppointment(Predicate<Appointment> match)
    {
        _logger.StartMethod(match);
        Appointment? appointment = GetAppointment(match);
        if(appointment == null)
        {
            _logger.StopMethod(false);
            return false;
        }

        appointment.Remove();
        _logger.StopMethod(true);
        return true;
    }

    public bool TryRemoveAppointment(string service, string center)
    {
        _logger.StartMethod(service, center);
        var result = TryRemoveAppointment(a => a.Service == service && a.Center == center);
        _logger.StopMethod(result);
        return result;
    }

    public bool TryRemoveAppointment(string service, string center, DateTime dateTime)
    {
        _logger.StartMethod(service, center, dateTime);
        bool timeMatch(DateTime time) => time.Year == dateTime.Year &&
                                        time.Month == dateTime.Month &&
                                        time.Day == dateTime.Day &&
                                        time.Hour == dateTime.Hour &&
                                        time.Minute == dateTime.Minute;
        var result = TryRemoveAppointment(a => a.Service == service && a.Center == center && timeMatch(a.DateTime));
        _logger.StopMethod(result);
        return result;
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
            Url = "http://www.valencia.es/QSIGE/apps/citaprevia/index.html#!/queryAppoinment"
        };

        _driver.Wait(TimeoutForLoading);

        IWebElement phoneElement = _driver.FindElement(By.Id("txtTelefono"));
        IWebElement phoneElementParent2 = _driver.GetElementParent(phoneElement, 2)!;

        if(phoneElementParent2.GetAttribute("className") != "form-group ng-hide")
        {
            _logger.LogError("Appointments page loaded wrong");
            throw new Exception("Appointments page loaded wrong.");
        }

        SetDocument(_document);
        SubmitDocument();

        _driver.Wait(TimeoutForLoading);

        WaitLoading(out Dialog _);
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

    private void SetDocument(string document)
    {
        _logger.StartMethod(document);
        IWebElement documentElement = _driver!.FindElement(By.Id("nif"));
        documentElement.SendKeys(document);
        _logger.StopMethod();
    }

    private void SubmitDocument()
    {
        _logger.StartMethod();
        IWebElement submitButton = _driver!.FindElement(By.XPath("html/body/div[2]/div/div[3]/div/div/div[1]/div[2]/form/div[3]/button"));
        submitButton.Submit();
        _logger.StopMethod();
    }

    public bool TryGetAppointment(int index, out Appointment? appointment)
    {
        _logger.StartMethod(index);
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
