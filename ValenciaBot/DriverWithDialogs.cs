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
    public const int TimeoutForLoading = 1000;
    protected IWebDriver? _driver = null;

    public DriverWithDialogs()
    {
    }
    public DriverWithDialogs(IWebDriver driver)
    {
        _driver = driver;
    }

    protected void WaitLoading(out Dialog? otherDialog, int delay = TimeoutForLoading)
    {
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
                return;
            }
        }
    }

    protected bool TryGetInfoDialog(out Dialog? dialog)
    {
        Dialog? foundDialog;
        WaitLoading(out foundDialog);
        if(foundDialog == null)
            WaitLoading(out foundDialog);

        if(foundDialog != null && foundDialog.IsInfoDialog)
        {
            dialog = foundDialog;
            return true;
        }


        dialog = null;
        return false;
    }

    protected bool TryGetDialog(out Dialog? dialog)
    {
        try
        {
            IWebElement dialogElement = _driver!.FindElement(By.XPath("html/body/div[3]"));
            dialog = new Dialog(dialogElement);
            if(dialog.Created == false)
            {
                dialog = null;
                return false;
            }
            return true;
        }
        catch(NoSuchElementException)
        {
            dialog = null;
            return false;
        }
    }

}
