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

    private readonly string _document = "761234567";


    public Appointments(string document)
    {
        _document = document;
    }

    private Appointment? GetAppointment(Predicate<Appointment> match)
    {
        Appointment[] appointments = GetAllApointments();
        int index = Array.FindIndex(appointments, match);
        return index < 0 ? null : appointments[index];
    }

    public Appointment? GetAppointment(string service, string center)
    {
        Logger.Log($"Getting Appointment with service: '{service}' center: '{center}'");
        return GetAppointment(a => a.Service == service && a.Center == center);
    }

    private int GetAppointmentIndex(Predicate<Appointment> match)
    {
        Appointment[] appointments = GetAllApointments();
        return Array.FindIndex(appointments, match);
    }

    public bool HasAppointment(Predicate<Appointment> match) =>
        GetAppointmentIndex(match) >= 0;

    public bool HasAppointment(string service, string center) =>
        HasAppointment(a => a.Service == service && a.Center == center);

    public Appointment[] GetAllApointments()
    {
        List<Appointment> result = new();
        int i = 0;
        while(TryGetAppointment(i, out Appointment? appointment))
        {
            result.Add(appointment!);
            i++;
        }

        return result.ToArray();
    }

    public bool TryRemoveAppointment(Predicate<Appointment> match)
    {
        Appointment? appointment = GetAppointment(match);
        if(appointment == null)
        {
            Logger.Log($"Appointment was not removed: not found");
            return false;
        }

        appointment.Remove();
        Logger.Log($"Appointment removed");

        return true;
    }

    public bool TryRemoveAppointment(string service, string center)
    {
        Logger.Log($"Trying to remove appointment with service: '{service}' center: '{center}'");
        return TryRemoveAppointment(a => a.Service == service && a.Center == center);
    }

    public bool TryRemoveAppointment(string service, string center, DateTime dateTime)
    {
        Logger.Log($"Trying to remove appointment with service: '{service}' center: '{center}' dateTime: '{dateTime:mm:hh dd:MM:yyyy}'");
        bool timeMatch(DateTime time) => time.Year == dateTime.Year &&
                                        time.Month == dateTime.Month &&
                                        time.Day == dateTime.Day &&
                                        time.Hour == dateTime.Hour &&
                                        time.Minute == dateTime.Minute;
        return TryRemoveAppointment(a => a.Service == service && a.Center == center && timeMatch(a.Time));
    }


    public void Open()
    {
        if(Opened)
            Close();
        _driver = new ChromeDriver
        {
            Url = "http://www.valencia.es/QSIGE/apps/citaprevia/index.html#!/queryAppoinment"
        };

        _driver.Wait(TimeoutForLoading);

        SetDocument(_document);
        SubmitDocument();

        _driver.Wait(TimeoutForLoading);

        WaitLoading(out Dialog _);
    }
    
    public void Close()
    {
        if(Opened == false)
            return;
        _driver!.Close();
        _driver = null;
    }

    private void SetDocument(string document)
    {
        IWebElement documentElement = _driver!.FindElement(By.Id("nif"));
        documentElement.SendKeys(document);
    }

    private void SubmitDocument()
    {
        IWebElement submitButton = _driver!.FindElement(By.XPath("html/body/div[2]/div/div[3]/div/div/div[1]/div[2]/form/div[3]/button"));
        submitButton.Submit();
    }

    public bool TryGetAppointment(int index, out Appointment? appointment)
    {
        try
        {
            IWebElement appointmentElement = _driver!.FindElement(By.XPath($"/html/body/div[2]/div/div[3]/div/div/div[2]/div[2]/table/tbody/tr[1]/td[{index + 1}]"));
            appointment = new Appointment(_driver, appointmentElement);
            return true;
        }
        catch(NoSuchElementException)
        {
            appointment = null;
            return false;
        }
    }

}
