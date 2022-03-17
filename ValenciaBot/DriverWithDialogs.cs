using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValenciaBot.WebDriverExtensions;

namespace ValenciaBot;

public class DriverWithDialogs
{
    protected ClassLogger _logger;

    public const int TimeoutForLoading = 1000;
    protected IWebDriver? _driver = null;

    public DriverWithDialogs()
    {
        _logger = new($"[{this.GetType().Name}] ");
    }
    public DriverWithDialogs(IWebDriver driver) : this()
    {
        _driver = driver;
    }

    protected void WaitLoading(out Dialog? otherDialog, int delay = TimeoutForLoading)
    {
        _logger.StartMethod(delay);
        Dialog? dialog;
        while(true)
        {
            _driver!.Wait(delay);
            if(TryGetDialog(out dialog))
            {
                if(dialog!.Created)
                {
                    if(dialog!.IsLoadingDialog)
                    {
                        continue;
                    }
                    else
                    {
                        otherDialog = dialog;
                        _logger.StopMethod("Found not loading dialog");
                        return;
                    }
                }
                else
                {
                    continue;
                }

            }
            else
            {
                otherDialog = null;
                _logger.StopMethod();
                return;
            }
        }
    }

    protected bool TryGetInfoDialog(out Dialog? dialog)
    {
        _logger.StartMethod();
        Dialog? foundDialog;
        WaitLoading(out foundDialog);
        if(foundDialog == null)
            WaitLoading(out foundDialog);

        if(foundDialog != null && foundDialog.IsInfoDialog)
        {
            dialog = foundDialog;
            _logger.StopMethod(true, dialog!.Content);
            return true;
        }


        dialog = null;
        _logger.StopMethod(false);
        return false;
    }

    protected bool TryGetDialog(out Dialog? dialog)
    {
        _logger.StartMethod();
        try
        {
            IWebElement dialogElement = _driver!.FindElement(By.XPath("html/body/div[3]"));
            dialog = new Dialog(dialogElement);
            if(dialog.Created == false)
            {
                dialog = null;
                _logger.StopMethod(false);
                return false;
            }
            _logger.StopMethod(true, dialog!.DialogType);
            return true;
        }
        catch(NoSuchElementException)
        {
            dialog = null;
            _logger.StopMethod(false);
            return false;
        }
    }

}
