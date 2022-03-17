using OpenQA.Selenium;
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

    public static SelectElement GetSelector(this IWebDriver driver, By by)
    {
        IWebElement selectorElement = driver.FindElement(by);
        return new SelectElement(selectorElement);
    }

    public static string GetElementXPath(this IWebDriver driver, IWebElement element)
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
        return (string)((IJavaScriptExecutor)driver).ExecuteScript(javaScript, element);
    }

    public static IWebElement? GetElementParent(this IWebDriver driver, IWebElement element, int times = 1)
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
}
