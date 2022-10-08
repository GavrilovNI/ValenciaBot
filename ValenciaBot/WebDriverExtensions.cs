using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace ValenciaBot.WebDriverExtensions;

public static class WebDriverExtensions
{
    // Causes the WebDriver to wait for at least a fixed delay
    public static void Wait(this IWebDriver driver, int delay) =>  driver.Wait(delay, delay);
    public static void Wait(this IWebDriver driver, int delay, int poolingInterval)
    {
        var end = DateTime.Now.AddMilliseconds(delay);
        WebDriverWait wait = new(driver, TimeSpan.FromMilliseconds(delay));
        wait.PollingInterval = TimeSpan.FromMilliseconds(poolingInterval);
        wait.Until(wd => DateTime.Now >= end);
    }

    public static bool ElementsEqual(this IJavaScriptExecutor executor, IWebElement a, IWebElement b)
    {
        return executor.GetElementXPath(a) == executor.GetElementXPath(b);
    }

    public static SelectElement? FindSelector(this ISearchContext context, By by)
    {
        IWebElement selectorElement = context.FindElement(by);
        return GetSelector(context, selectorElement);
    }

    public static SelectElement? GetSelector(this ISearchContext context, IWebElement selectorElement)
    {
        if(selectorElement == null)
            return null;
        try
        {
            return new SelectElement(selectorElement);
        }
        catch(Exception ex) when(ex is InvalidOperationException || ex is UnexpectedTagNameException)
        {
            return null;
        }
    }

    public static string GetElementXPath(this IJavaScriptExecutor executor, IWebElement element)
    {
        string javaScript = "function getElementXPath(elt){" +
                                "var path = \"\";" +
                                "for (; elt && elt.nodeType == 1; elt = elt.parentNode){" +
                                    "idx = getElementIdx(elt);" +
                                    "xname = elt.tagName;" +
                                    "if (idx > 1){" +
                                        "xname += \"[\" + idx + \"]\";" +
                                    "}" +
                                    "path = \"/\" + xname + path;" +
                                "}" +
                                "return path;" +
                            "}" +
                            "function getElementIdx(elt){" +
                                "var count = 1;" +
                                "for (var sib = elt.previousSibling; sib ; sib = sib.previousSibling){" +
                                    "if(sib.nodeType == 1 && sib.tagName == elt.tagName){" +
                                        "count++;" +
                                    "}" +
                                "}" +
                                "return count;" +
                            "}" +
                            "return getElementXPath(arguments[0]).toLowerCase();";
        return (string)executor.ExecuteScript(javaScript, element);
    }

    public static IWebElement? GetElementParent<T>(this T driver, IWebElement element, int times = 1) where T : IJavaScriptExecutor, ISearchContext
    {
        string xPath = driver.GetElementXPath(element);

        for(int i = 0; i < times; i++)
        {
            int lastSlash = xPath.LastIndexOf('/');
            if(lastSlash == -1)
                return null;
            else
                xPath = xPath[..lastSlash];
        }
        return driver.FindElement(By.XPath(xPath));
    }

    public static string CreateNewWindow<T>(this T driver) where T : IJavaScriptExecutor, IWebDriver
    {
        driver.ExecuteScript("window.open();");
        return driver.WindowHandles.Last();
    }
}
