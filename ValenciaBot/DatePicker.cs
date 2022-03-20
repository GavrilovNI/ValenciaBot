using OpenQA.Selenium;
using System.Linq;
using ValenciaBot.WebDriverExtensions;
using ValenciaBot.DateTimeExtensions;

namespace ValenciaBot;

public class DatePicker : DriverWithDialogs<IWebDriver>
{
    private const string _activeClass = "day ng-binding ng-scope active";
    private const string _avaliableClass = "day ng-binding ng-scope";
    private const string _unavaliableClass = "day ng-binding ng-scope disabled";
    private const string _pastDaysClass = "day ng-binding ng-scope past disabled";
    private const string _currenDayClass = "day ng-binding ng-scope current disabled";

    private readonly ITab _iTab;
    private readonly By _dropdownBy;

    private IWebElement Dropdown
    {
        get
        {
            _iTab.Open();
            return _driver.FindElement(_dropdownBy);
        }
    }

    private IWebElement ShowCalendarButton => Dropdown.FindElement(By.XPath("a/div/div/button"));

    private IWebElement Table => Dropdown.FindElement(By.XPath("ul/div/table"));

    private IWebElement YearAndMonthButton => Table.FindElement(By.XPath("thead/tr[1]/th[2]"));
    private IWebElement LeftArrow => Table.FindElement(By.XPath("thead/tr[1]/th[1]"));
    private IWebElement RightArrow => Table.FindElement(By.XPath("thead/tr[1]/th[3]"));

    public bool IsOpen => Dropdown.GetAttribute("className") == "dropdown open";
    public bool DayPicked => Dropdown.FindElement(By.XPath("a/div/input")).GetAttribute("className").Contains("ng-empty") == false;

    private (int x, int y) _calendarSize = (7, 6);

    private readonly string[] _monthAbbreviations = new string[12] { "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic" };

    public DatePicker(IWebDriver driver, ITab iTab, By dropdownBy) : base(driver)
    {
        _logger.StartMethod(driver, iTab, dropdownBy);
        _iTab = iTab;
        _dropdownBy = dropdownBy;
        _logger.StopMethod();
    }

    public void Open()
    {
        if(IsOpen == false)
            ShowCalendarButton.Click();
    }

    public void Close()
    {
        if(IsOpen)
            ShowCalendarButton.Click();
    }

    public DateOnly GetCurrentYearAndMonth()
    {
        string stringDate = YearAndMonthButton.Text;
        string stringYear = stringDate[..4];
        string stringMonth = stringDate.Substring(5, 3);

        int year = int.Parse(stringYear);
        int month = Array.FindIndex(_monthAbbreviations, x => x == stringMonth);
        if(month == -1)
            throw new ArgumentException($"Couldn't find month by abbreviation '{stringMonth}'");
        else
            month++;

        return new DateOnly(year, month, 1);
    }

    public void MoveToNextMonth()
    {
        RightArrow.Click();
    }

    public void MoveToPrevMonth()
    {
        LeftArrow.Click();
    }

    public void GoToYearAndMonth(DateOnly dateTime)
    {
        DateOnly currentDateTime = GetCurrentYearAndMonth();
        int monthDifference = currentDateTime.MonthDifference(dateTime);
        IWebElement button = monthDifference > 0 ? RightArrow : LeftArrow;
        monthDifference = (int)MathF.Abs(monthDifference);

        for(int i = 0; i < monthDifference; i++)
            button.Click();
    }

    private IWebElement GetDayButton(int day)
    {
        (int x, int y) = GetDayPosition(day);
        return GetDayButton(x, y);
    }
    private IWebElement GetDayButton(int x, int y)
    {
        return Table.FindElement(By.XPath($"tbody/tr[{y + 1}]/td[{x + 1}]"));
    }

    public void PickDay(DateOnly dateOnly)
    {
        GoToYearAndMonth(dateOnly);
        PickDay(dateOnly.Day);
        WaitLoading(out Dialog _);
    }

    private void PickDay(int day)
    {
        GetDayButton(day).Click();
    }

    public bool TryGetFirstAvaliableDay(out DateOnly dateTime, DateOnly from, DateOnly before)
    {
        if(from >= before)
        {
            dateTime = before;
            return false;
        }
        DateOnly current = from;
        GoToYearAndMonth(from);
        while(current < before)
        {
            int fromDay = 1;
            int beforeDay = 32;
            if(current.Year == from.Year && current.Month == from.Month)
                fromDay = from.Day;
            if(current.Year == before.Year && current.Month == before.Month)
                beforeDay = before.Day;

            if(TryGetFirstAvaliableDayInCurrentMonth(out dateTime, fromDay, beforeDay))
                return true;

            current = current.AddMonths(1);
            MoveToNextMonth();
        }

        dateTime = before;
        return false;
    }

    public bool TryGetFirstAvaliableDayInCurrentMonth(out DateOnly dateTime, int fromDay = 1, int beforeDay = 32)
    {
        DateOnly currentYearAndMonth = GetCurrentYearAndMonth();
        int daysInMonth = DateTime.DaysInMonth(currentYearAndMonth.Year, currentYearAndMonth.Month);
        for(int i = fromDay - 1; i < daysInMonth && i < beforeDay - 1; i++)
        {
            int day = i + 1;
            if(IsDayAvaliable(day))
            {
                dateTime = new DateOnly(currentYearAndMonth.Year, currentYearAndMonth.Month, day);
                return true;
            }
        }

        dateTime = currentYearAndMonth.AddDays(beforeDay - 1);
        return false;
    }

    private (int x, int y) GetDayPosition(int day)
    {
        day--;

        (int firstX, int firstY) = GetPositionOfFirstDay();

        int x = (firstX + day) % _calendarSize.x;
        int y = firstY + (firstX + day) / _calendarSize.x;
        return (x, y);
    }

    public bool IsDayAvaliable(DateOnly dateOnly)
    {
        GoToYearAndMonth(dateOnly);
        return IsDayAvaliable(dateOnly.Day);
    }

    private bool IsDayAvaliable(int day)
    {
        (int x, int y) = GetDayPosition(day);
        return IsDayAvaliable(x, y);
    }

    private bool IsDayAvaliable(int x, int y)
    {
        string currentClass = GetDayButton(x, y).GetAttribute("class");

        if(currentClass == _avaliableClass || currentClass == _activeClass)
            return true;
        else if(currentClass == _pastDaysClass || currentClass == _currenDayClass || currentClass == _unavaliableClass)
            return false;
        else
            throw new InvalidElementStateException("Day button has unknown class.");
    }

    private (int x, int y) GetPositionOfFirstDay()
    {
        for(int y = 0; y < _calendarSize.y; y++)
        {
            for(int x = 0; x < _calendarSize.x; x++)
            {
                IWebElement dayButton = GetDayButton(x, y);
                int day = int.Parse(dayButton.Text);
                if(day == 1)
                    return (x, y);
            }
        }
        throw new NotFoundException("First day button nor found.");
    }
}
