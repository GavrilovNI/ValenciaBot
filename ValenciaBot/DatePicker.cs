using OpenQA.Selenium;
using System.Linq;
using ValenciaBot.WebDriverExtensions;
using ValenciaBot.DateTimeExtensions;

namespace ValenciaBot;

public class DatePicker : DriverWithDialogs<IWebDriver>
{
    public record Vector2(int X, int Y);

    private const string _activeClass = "day ng-binding ng-scope active";
    private const string _avaliableClass = "day ng-binding ng-scope";
    private const string _unavaliableClass = "day ng-binding ng-scope disabled";
    private const string _pastDaysClass = "day ng-binding ng-scope past disabled";
    private const string _currenDayClass = "day ng-binding ng-scope current disabled";

    private readonly ITab _iTab;

    private IWebElement? _dropDown;
    private IWebElement? _showCalendarButton;
    private IWebElement? _table;
    private IWebElement? _yearAndMonthButton;
    private IWebElement? _leftArrow;
    private IWebElement? _rightArrow;

    private Vector2? _firstDayOfMonthPosition;
    private Vector2 FirstDayOfMonthPosition
    {
        get
        {
            bool stillFirst = GetDayButton(_firstDayOfMonthPosition!).GetAttribute("innerHTML") == "1";
            if(stillFirst == false)
                _firstDayOfMonthPosition = GetPositionOfFirstDay();
            return _firstDayOfMonthPosition!;
        }
        set
        {
            bool isFirst = GetDayButton(value!).GetAttribute("innerHTML") == "1";
            if(isFirst == false)
                throw new ArgumentException("Not first day.");
            else
                _firstDayOfMonthPosition = value;
        }
    }

    public bool IsOpen
    {
        get
        {
            if(_iTab.Opened == false)
                _iTab.Open();
            return _dropDown!.GetAttribute("className") == "dropdown open";
        }
    }
    public bool DayPicked
    {
        get
        {
            if(_iTab.Opened == false)
                _iTab.Open();
            return _dropDown!.FindElement(By.XPath("a/div/input")).GetAttribute("className").Contains("ng-empty") == false;
        }
    }

    private (int x, int y) _calendarSize = (7, 6);

    private readonly string[] _monthAbbreviations = new string[12] { "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic" };

    public DatePicker(IWebDriver driver, ITab iTab, By dropdownBy) : base(driver)
    {
        _logger.StartMethod(driver, iTab, dropdownBy);
        _iTab = iTab;
        Update(dropdownBy);
        _logger.StopMethod();
    }

    public void Update(By dropdownBy)
    {
        _iTab.Open();
        _dropDown = _driver.FindElement(dropdownBy);
        _showCalendarButton = _dropDown.FindElement(By.XPath("a/div/div/button"));
        _table = _dropDown.FindElement(By.XPath("ul/div/table"));
        _yearAndMonthButton = _table.FindElement(By.XPath("thead/tr[1]/th[2]"));
        _leftArrow = _table.FindElement(By.XPath("thead/tr[1]/th[1]"));
        _rightArrow = _table.FindElement(By.XPath("thead/tr[1]/th[3]"));

        FirstDayOfMonthPosition = GetPositionOfFirstDay();
    }

    public void Open()
    {
        if(IsOpen == false)
            _showCalendarButton!.Click();
    }
    public void Close()
    {
        if(IsOpen)
            _showCalendarButton!.Click();
    }

    public DateOnly GetCurrentYearAndMonth()
    {
        if(_iTab.Opened == false)
            _iTab.Open();
        string stringDate = _yearAndMonthButton!.Text;
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
        if(_iTab.Opened == false)
            _iTab.Open();
        _rightArrow!.Click();
        FirstDayOfMonthPosition = GetPositionOfFirstDay();
    }
    public void MoveToPrevMonth()
    {
        if(_iTab.Opened == false)
            _iTab.Open();
        _leftArrow!.Click();
        FirstDayOfMonthPosition = GetPositionOfFirstDay();
    }

    public void GoToYearAndMonth(DateOnly dateTime)
    {
        DateOnly currentDateTime = GetCurrentYearAndMonth();
        int monthDifference = currentDateTime.MonthDifference(dateTime);
        IWebElement button = monthDifference > 0 ? _rightArrow! : _leftArrow!;
        monthDifference = (int)MathF.Abs(monthDifference);

        for(int i = 0; i < monthDifference; i++)
            button!.Click();

        if(monthDifference > 0)
            FirstDayOfMonthPosition = GetPositionOfFirstDay();
    }

    private Vector2 GetPositionOfFirstDay()
    {
        for(int y = 0; y < _calendarSize.y; y++)
        {
            for(int x = 0; x < _calendarSize.x; x++)
            {
                IWebElement dayButton = GetDayButton(new Vector2(x, y));
                int day = int.Parse(dayButton.GetAttribute("innerHTML"));
                if(day == 1)
                    return new(x, y);
            }
        }
        throw new NotFoundException("First day button nor found.");
    }

    private Vector2 GetDayPosition(int dayInMonth)
    {
        dayInMonth--;
        int x = (FirstDayOfMonthPosition.X + dayInMonth) % _calendarSize.x;
        int y = FirstDayOfMonthPosition.Y + (FirstDayOfMonthPosition.X + dayInMonth) / _calendarSize.x;
        return new Vector2(x, y);
    }
    private IWebElement GetDayButton(Vector2 position)
    {
        return _table.FindElement(By.XPath($"tbody/tr[{position.Y + 1}]/td[{position.X + 1}]"));
    }
    private IWebElement GetDayButton(int dayInMonth)
    {
        Vector2 position = GetDayPosition(dayInMonth);
        return GetDayButton(position);
    }
    
    public void PickDay(DateOnly dateOnly)
    {
        GoToYearAndMonth(dateOnly);
        PickDay(dateOnly.Day);
    }
    private void PickDay(int dayinMonth)
    {
        PickDay(GetDayButton(dayinMonth));
    }
    private void PickDay(IWebElement dayButton)
    {
        dayButton.Click();
        WaitLoading(out Dialog _);
    }

    private static bool IsDayAvaliable(IWebElement dayButton)
    {
        string currentClass = dayButton.GetAttribute("class");

        if(currentClass == _avaliableClass || currentClass == _activeClass)
            return true;
        else if(currentClass == _pastDaysClass || currentClass == _currenDayClass || currentClass == _unavaliableClass)
            return false;
        else
            throw new InvalidElementStateException("Day button has unknown class.");
    }
    public bool IsDayAvaliable(DateOnly exactDate)
    {
        GoToYearAndMonth(exactDate);
        return IsDayAvaliable(GetDayButton(exactDate.Day));
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
            int dayOfMonth = i + 1;
            IWebElement dayElement = GetDayButton(dayOfMonth);
            if(IsDayAvaliable(dayElement))
            {
                dateTime = new DateOnly(currentYearAndMonth.Year, currentYearAndMonth.Month, dayOfMonth);
                return true;
            }
        }

        dateTime = currentYearAndMonth.AddDays(beforeDay - 1);
        return false;
    }
}
