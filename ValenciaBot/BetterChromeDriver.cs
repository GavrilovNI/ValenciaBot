using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;
public class BetterChromeDriver : IWebDriver, IDisposable, IJavaScriptExecutor
{
    private ClassLogger _logger = new(nameof(BetterChromeDriver));

    private ChromeDriver? _driver = null;

    private ChromeDriver? Driver
    {
        get
        {
            if (_driver == null)
                return null;

            if(CheckIfDriverReachable() == false)
                _logger.LogError("Driver is not reachable.");
            return _driver;
        }
        set => _driver = value;
    }

    public bool Opened => Driver != null && Driver!.WindowHandles.Count > 0;

    public int TabsCount => Opened ? WindowHandles.Count : 0;
    public string? CurrentTab
    {
        get
        {
            FixTabs();
            return Opened ? Driver!.CurrentWindowHandle : null;
        }
    }

    public string Url
    {
        get
        {
            FixTabs();
            return Opened ? Driver!.Url : null!;
        }
        set
        {
            FixTabs();
            if(Opened == false)
                Open();
            Driver!.Url = value;
        }
    }
    public string Title
    {
        get
        {
            FixTabs();
            return Opened ? Driver!.Title : null!;
        }
    }
    public string PageSource
    {
        get
        {
            FixTabs();
            return Opened ? Driver!.PageSource : null!;
        }
    }

    public ReadOnlyCollection<string> WindowHandles =>
        Driver != null ? Driver!.WindowHandles : new ReadOnlyCollection<string>(new List<string>());

        
    public string CurrentWindowHandle => CurrentTab!;

    private void FixTabs()
    {
        if(Opened == false)
            return;

        string tab;
        try
        {
            tab = Driver!.CurrentWindowHandle;
        }
        catch(NoSuchWindowException)
        {
            Driver!.SwitchTo().Window(Driver?.WindowHandles[0]);
        }
    }

    private bool CheckIfDriverReachable()
    {
        if(_driver == null)
            return false;
        try
        {
            var test = _driver.WindowHandles;
            return true;
        }
        catch (WebDriverException)
        {
            _driver.Quit();
            _driver = null;
            return false;
        }
    }

    public string Open()
    {
        if(Opened)
            Close();
        Driver = new();
        return Driver!.CurrentWindowHandle;
    }

    public void Close()
    {
        if(CheckIfDriverReachable() == false)
            return;
        int tabsCount = TabsCount;
        for(int i = 0; i < tabsCount; i++)
        {
            _driver?.SwitchTo().Window(Driver?.WindowHandles[0]);
            _driver?.Close();
        }
        _driver?.Quit();
        _driver = null;
    }

    public bool TabExists(string tab)
    {
        FixTabs();
        return Opened && Driver!.WindowHandles.Contains(tab);
    }

    public string CreateTab()
    {
        FixTabs();
        return Opened? this.CreateNewWindow() : Open();
    }

    public void SetTab(string tab)
    {
        FixTabs();
        if(TabExists(tab) == false)
            throw new InvalidOperationException("Tab doesn't exist.");
        Driver!.SwitchTo().Window(tab);
    }

    public void CloseTab(string tab)
    {
        FixTabs();
        if(TabExists(tab) == false)
            throw new InvalidOperationException("Tab doesn't exist.");
        int tabsCount = TabsCount;

        if(tabsCount == 1)
        {
            Close();
        }
        else
        {
            SetTab(tab);
            Driver!.Close();
            Driver!.SwitchTo().Window(Driver?.WindowHandles[0]);
        }
    }

    public IWebElement FindElement(By by)
    {
        FixTabs();
        return Driver!.FindElement(by);
    }
    public ReadOnlyCollection<IWebElement> FindElements(By by)
    {
        FixTabs();
        return Driver!.FindElements(by);
    }
    public void Dispose()
    {
        FixTabs();
        Driver?.Dispose();
    }

    public void Quit() => Close();
    public IOptions Manage()
    {
        FixTabs();
        return Driver?.Manage()!;
    }
    public INavigation Navigate()
    {
        FixTabs();
        return Driver?.Navigate()!;
    }
    public ITargetLocator SwitchTo()
    {
        FixTabs();
        return Driver!.SwitchTo();
    }

    public object ExecuteScript(string script, params object[] args)
    {
        FixTabs();
        return Driver!.ExecuteScript(script, args);
    }
    public object ExecuteScript(PinnedScript script, params object[] args)
    {
        FixTabs();
        return Driver!.ExecuteScript(script, args);
    }
    public object ExecuteAsyncScript(string script, params object[] args)
    {
        FixTabs();
        return Driver!.ExecuteScript(script, args);
    }
}
