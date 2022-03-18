using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using ValenciaBot.DateTimeExtensions;
using ValenciaBot.DirectoryExtensions;

namespace ValenciaBot;

public class Program
{
    public static readonly string LogPath = DirectoryExt.ProjectDirectory!.Parent!.FullName + "\\logs\\log.log";
    private static readonly ClassLogger _logger = new(nameof(Program));

    private static Program[] _programs = Array.Empty<Program>();
    public static Program[] Programs => _programs;
    
    private static readonly string _telegramBotSunscribersFile = DirectoryExt.ProjectDirectory!.Parent!.FullName + "\\botSubs.txt";
    private static readonly string _telegramBotTokenFile = DirectoryExt.ProjectDirectory!.Parent!.FullName + "\\telegramBotToken.txt";

    private static TelegramBot? _bot;

    private static void CreateTelegramBot()
    {
        string telegramBotTokenFileFullPath = Path.GetFullPath(_telegramBotTokenFile);
        if(File.Exists(telegramBotTokenFileFullPath))
        {
            string botToken = File.ReadAllLines(telegramBotTokenFileFullPath)[0];
            _bot = string.IsNullOrEmpty(botToken) ? null : new TelegramBot(botToken, _telegramBotSunscribersFile);
            if(_bot == null)
                _logger.LogWarning("Telegram bot was not created. Token inside token file not found. It should be placed on first line");
            else
                _logger.Log($"Telegram bot created. Token: '{botToken}'");
        }
        else
        {
            _logger.LogWarning("Telegram bot was not created. Token file not found");
        }
    }

    public static void Main()
    {
        AppointmentInfo testInfo = new AppointmentInfo()
        {
            Location = new LocationInfo()
            {
                Service = "ATENCION ESPECIALIZADA-LICENCIAS, INOCUAS,CONTENEDORES",
                Center = "JUNTA DE DISTRITO ABASTOS",
            },
            Name = "Name",
            Surname = "Surname",
            DocumentType = "Pasaporte",
            Document = "761234566",
            PhoneNumber = "681123456",
            Email = "email@email.com"
        };

        AppointmentInfo info = new()
        {
            Location = new LocationInfo()
            {
                Service = "PADRON CP - Periodista Azzati",
                Center = "PADRON Periodista Azzati 2",
            },
            Name = "Name",
            Surname = "Surname",
            DocumentType = "Pasaporte",
            Document = "761234566",
            PhoneNumber = "681123456",
            Email = "email@email.com"
        };

        DateOnly beforeDate = new(2022, 6, 13);

        //CreateTelegramBot();
        _programs = new Program[]
        {
            new Program(info, beforeDate),
        };
        /*_programs = new Program[]
        {
            new Program(info, beforeDate),
            //new Program("PADRON CP - OAC Tabacalera", "OAC TABACALERA"),
            //new Program("PADRON CP - Juntas Municipales", "JUNTA DE DISTRITO ABASTOS"),
            //new Program("PADRON CP - Juntas Municipales", "JUNTA DE DISTRITO MARITIMO"),
            //new Program("PADRON CP - Juntas Municipales", "JUNTA DE DISTRITO TRANSITS"),
        };*/

        _bot?.SendMessageToSubscribers("Bot started.");
        foreach (Program program in Programs)
            program.Start();
        Console.ReadKey();
        foreach(Program program in Programs)
            program.Stop();
        _bot?.SendMessageToSubscribers("Bot stopped.");
    }

    public readonly AppointmentInfo Info;
    private readonly DateOnly _beforeDateOriginal = new(2022, 6, 13);

    private Appointments? _appointments;
    private AppointmentCreator? _creator;

    private readonly int _delayInSeconds = 5;
    private System.Timers.Timer? _timer = null;
    public bool Running => _timer != null;

    public DateTime? ExistingAppointment { get; private set; } = null;

    private DateOnly BeforeDate => ExistingAppointment.HasValue ? ExistingAppointment.Value.ToDateOnly() : _beforeDateOriginal;

    private readonly BetterChromeDriver _driver;

    public Program(AppointmentInfo info, DateOnly beforeDate)
    {
        _logger.Log($"Program created. Appointment info: '{info}'. Before date: '{beforeDate}'");
        Info = info;
        _beforeDateOriginal = beforeDate;

        _driver = new BetterChromeDriver();
    }

    private bool TryCreateBetterAppointment()
    {
        _logger.StartMethod();
        try
        {
            DateTime? oldAppointment = _appointments!.GetAppointment(Info.Document, Info.Location)?.DateTime;
            if(oldAppointment == null && ExistingAppointment != null)
            {
                _bot?.SendMessageToSubscribers("Warning: Existing appointment was removed since last check.");
                _logger.LogWarning("Existing appointment was removed since last check");
            }
            ExistingAppointment = oldAppointment;

            DateOnly? avaliableDate = _creator!.GetFirstAvaliableDate(Info.Location, BeforeDate);
            if(avaliableDate is null)
            {
                //_bot?.SendMessageToSubscribers("Better date not found.");
                _logger.StopMethod(true, "Better date not found");
                return true;
            }
            else
            {
                _logger.Log($"Found avaliable date {avaliableDate}");
                _bot?.SendMessageToSubscribers($"Found avaliable date {avaliableDate}.");

                if(ExistingAppointment.HasValue)
                {
                    bool appointmentRemoved = _appointments!.TryRemoveAppointment(Info.Document, Info.Location);
                    if(appointmentRemoved == false)
                    {
                        _bot?.SendMessageToSubscribers("Error: Appointment was not removed. But it exists.");
                        _logger.StopMethod(false, "Appointment was not removed. But it exists");
                        return false;
                    }
                }

                bool created = _creator.CreateAppointmentByExactDate(Info, avaliableDate.Value, out DateTime createdTime);
                if(created)
                {
                    DateTime? createdAppointmentDate = _appointments!.GetAppointment(Info.Document, Info.Location)?.DateTime;
                    created = createdAppointmentDate == createdTime;
                }
                if(created)
                {
                    ExistingAppointment = createdTime;
                    _bot?.SendMessageToSubscribers($"New appointment created! Date: {createdTime}. Info : {Info}'.");
                    _logger.StopMethod(true, "----Created new appointment----", createdTime, Info);
                    return true;
                }
                else
                {
                    _bot?.SendMessageToSubscribers($"Error: Appointment was not created but time found. Found time: {avaliableDate}.");
                    _logger.StopMethod(false, "----Appointment was not created but time found----", avaliableDate);
                    return false;
                }
            }
        }
        catch(Exception ex)
        {
            _logger.LogError($"{ex.Message} {ex.StackTrace}");
            _driver.Close();
            _logger.StopMethod(false, ex);
            return false;
        }
    }
    public void Start()
    {
        if(Running)
            return;
        _logger.StartMethod();

        _appointments = new Appointments(_driver);
        _creator = new AppointmentCreator(_driver);

        void TimerTask(object? o, object? a)
        {
            _logger.StartMethod();
            if(_timer != null)
                _timer.Stop();

            bool needToBeRestartedImmidiately = false;
            do
            {
                bool doneFine = TryCreateBetterAppointment();
                needToBeRestartedImmidiately = doneFine == false;
                _logger.Log($"needToBeRestartedImmidiately :{needToBeRestartedImmidiately}");
            }
            while(needToBeRestartedImmidiately && _timer != null);

            if(_timer != null)
                _timer.Start();
            _logger.StopMethod();
        }

        _timer = new System.Timers.Timer(_delayInSeconds * 1000);
        _timer.Elapsed += TimerTask;
        _timer.AutoReset = false;

        TimerTask(null, null);

        _logger.StopMethod();
    }

    public void Stop()
    {
        if(Running == false)
            return;
        _logger.StartMethod();

        _timer!.Stop();
        _timer!.Dispose();
        _timer = null;
        _appointments!.Close();
        _creator!.Close();

        _driver!.Close();

        _logger.StopMethod();
    }
}
