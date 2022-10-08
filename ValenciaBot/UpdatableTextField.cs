using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;
public class UpdatableTextField
{
    protected static readonly Exception WebElementNotAvailableException = new InvalidOperationException("Web element is not available");

    protected ClassLogger _logger;

    private BetterChromeDriver _driver;
    private readonly ITab _iTab;
    private Func<IWebElement?> _elementGetter;

    private IWebElement? _element;

    public IWebElement? WebElement
    {
        get
        {
            if(_iTab.Opened == false)
                _iTab.Open();
            if(_element == null)
                Update();
            return _element;
        }
    }

    public UpdatableTextField(BetterChromeDriver driver, ITab tab, Func<IWebElement> elementGetter)
    {
        _logger = new(GetType().Name);
        _driver = driver;
        _iTab = tab;
        _elementGetter = elementGetter;
    }

    public UpdatableTextField(BetterChromeDriver driver, ITab tab, By elementBy) : this(driver, tab, () => driver.FindElement(elementBy))
    {
    }

    public void Update()
    {
        _logger.StartMethod();
        if(_iTab.Opened == false)
            _iTab.Open();
        _element = _elementGetter.Invoke();
        _logger.StopMethod();
    }

    public void SetTextFieldValue(string value)
    {
        _logger.StartMethod(value);
        var inputField = _element;
        if(inputField == null)
            throw WebElementNotAvailableException;
        inputField.Clear();
        inputField.SendKeys(value);
        _logger.StopMethod();
    }

    public string GetTextFieldValue()
    {
        _logger.StartMethod();
        var inputField = _element;
        if(inputField == null)
            throw WebElementNotAvailableException;
        var result = inputField.GetAttribute("value");
        _logger.StopMethod(result);
        return result;
    }
}
