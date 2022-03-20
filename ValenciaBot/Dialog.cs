using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValenciaBot;

public class Dialog
{
    private readonly IWebElement _dialog;

    public IWebElement WebElement => _dialog;

    public string DialogType { get; private set; } = String.Empty;
    public string Content { get; private set; } = String.Empty;

    public bool Created { get; private set; }

    public bool IsLoadingDialog => DialogType == "custom-dialog-loading";
    public bool IsInfoDialog => DialogType == "custom-dialog-header information";


    public Dialog(IWebElement dialog)
    {
        _dialog = dialog;

        try
        {
            IWebElement typeDiv = _dialog.FindElement(By.XPath("div[2]/div[1]"));
            DialogType = typeDiv.GetAttribute("className");
            try
            {
                Content = _dialog.FindElement(By.XPath("div[2]/div[2]/span")).Text;
            }
            catch(NoSuchElementException)
            {
                Content = _dialog.FindElement(By.XPath("div[2]/div[2]/p[2]")).Text;
            }
            Created = true;
        }
        catch(StaleElementReferenceException _)
        {
            Created = false;
        }

    }

    public void Confirm()
    {
        IWebElement closeButton = _dialog.FindElement(By.XPath("div[2]/div[3]/button[1]"));
        closeButton.Click();
    }

    public void Close()
    {
        IWebElement closeButton;
        try
        {
            closeButton = _dialog.FindElement(By.XPath("div[2]/div[3]/button[2]"));
        }
        catch
        {
            closeButton = _dialog.FindElement(By.XPath("div[2]/div[3]/button[1]"));
        }
        closeButton.Click();
    }
    
}
