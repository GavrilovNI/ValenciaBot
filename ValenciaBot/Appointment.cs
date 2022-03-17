using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValenciaBot;
public class Appointment : DriverWithDialogs
{
    private IWebElement _appointment;

    public DateTime DateTime
    {
        get
        {
            string time = _appointment.FindElement(By.XPath("p[1]")).Text;
            return DateTime.ParseExact(time, "dd/MM/yyyy - HH:mm", CultureInfo.InvariantCulture);
        }
    }
    public string Center
    {
        get
        {
            string center = _appointment.FindElement(By.XPath("p[2]")).Text;
            center = center.Substring("Centre: ".Length);
            return center;
        }
    }
    public string Service
    {
        get
        {
            string service = _appointment.FindElement(By.XPath("p[3]")).Text;
            service = service.Substring("Servici: ".Length);
            return service;
        }
    }

    public Appointment(IWebDriver driver, IWebElement appointment) : base(driver)
    {
        _appointment = appointment;
    }

    public void Remove()
    {
        Logger.Log("Appointment Remove() invoked");
        IWebElement removeButton = _appointment.FindElement(By.XPath("div/button[1]"));
        removeButton.Click();
        if(TryGetInfoDialog(out Dialog? dialog))
            dialog!.Confirm();
    }
}
