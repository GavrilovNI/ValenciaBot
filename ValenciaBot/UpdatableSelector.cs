using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;
public class UpdatableSelector
{
    protected static readonly Exception SelectorNotAvailableException = new InvalidOperationException("Selector is not available");

    protected ClassLogger _logger;

    private BetterChromeDriver _driver;
    private Func<IWebElement?> _elementGetter;

    private IWebElement? _element;
    private SelectElement? _selectElement;

    private readonly ITab _iTab;

    public SelectElement? SelectElement
    {
        get
        {
            if(_iTab.Opened == false)
                _iTab.Open();
            if(_element == null || _selectElement == null)
                Update();
            return _selectElement;
        }
    }

    public UpdatableSelector(BetterChromeDriver driver, ITab tab, Func<IWebElement> elementGetter)
    {
        _logger = new(GetType().Name);
        _iTab = tab;
        _driver = driver;
        _elementGetter = elementGetter;
    }

    public UpdatableSelector(BetterChromeDriver driver, ITab tab, By elementBy) : this(driver, tab, () => driver.FindElement(elementBy))
    {
    }

    public void Update()
    {
        _logger.StartMethod();
        if(_iTab.Opened == false)
            _iTab.Open();
        _element = _elementGetter.Invoke();
        _selectElement = _driver.GetSelector(_element);
        _logger.StopMethod();
    }

    public void SetSelectorValueByIndex(int index)
    {
        _logger.StartMethod(index);
        var selectElement = SelectElement;
        if(selectElement == null)
            throw SelectorNotAvailableException;
        selectElement.SelectByIndex(index);
        _logger.StopMethod();
    }

    public int GetSelectorValueIndex()
    {
        _logger.StartMethod();

        var result = -1;

        var selectElement = SelectElement;
        if(selectElement == null)
            throw SelectorNotAvailableException;
        int optionsCount = selectElement.Options.Count;
        for(int i = 0; i < optionsCount; i++)
        {
            if(_driver.ElementsEqual(selectElement.Options[i], selectElement.SelectedOption))
            {
                result = i;
                break;
            }
        }

        _logger.StopMethod(result);
        return result;
    }

    public void SetSelectorValueByText(string value)
    {
        _logger.StartMethod(value);
        var selectElement = SelectElement;
        if(selectElement == null)
            throw SelectorNotAvailableException;
        selectElement.SelectByText(value);
        _logger.StopMethod();
    }

    public string GetSelectorValueText()
    {
        _logger.StartMethod();
        var selectElement = SelectElement;
        if(selectElement == null)
            throw SelectorNotAvailableException;
        var result = selectElement.SelectedOption.GetAttribute("innerHTML");
        _logger.StopMethod(result);
        return result;
    }

}
