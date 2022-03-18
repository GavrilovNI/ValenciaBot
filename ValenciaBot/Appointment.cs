using OpenQA.Selenium;
using System.Globalization;

namespace ValenciaBot;
public class Appointment : DriverWithDialogs<IWebDriver>
{
    private readonly IWebElement _appointment;

    public DateTime DateTime
    {
        get
        {
            string time = _appointment.FindElement(By.XPath("p[1]")).Text;
            return DateTime.ParseExact(time, "dd/MM/yyyy - HH:mm", CultureInfo.InvariantCulture);
        }
    }

    public LocationInfo Location => new LocationInfo(Service, Center);

    public string Service
    {
        get
        {
            string service = _appointment.FindElement(By.XPath("p[3]")).Text;
            service = service["Servici: ".Length..];
            return service;
        }
    }
    public string Center
    {
        get
        {
            string center = _appointment.FindElement(By.XPath("p[2]")).Text;
            center = center["Centre: ".Length..];
            return center;
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
