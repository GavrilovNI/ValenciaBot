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

    public static Program Programm { get; private set; }
    
    private static readonly string _telegramBotSunscribersFile = DirectoryExt.ProjectDirectory!.Parent!.FullName + "\\botSubs.txt";
    private static readonly string _telegramBotTokenFile = DirectoryExt.ProjectDirectory!.Parent!.FullName + "\\telegramBotToken.txt";

    public static TelegramBot? Bot { get; private set; }

    private static void CreateTelegramBot()
    {
        string telegramBotTokenFileFullPath = Path.GetFullPath(_telegramBotTokenFile);
        if(File.Exists(telegramBotTokenFileFullPath))
        {
            string botToken = File.ReadAllLines(telegramBotTokenFileFullPath)[0];
            Bot = string.IsNullOrEmpty(botToken) ? null : new TelegramBot(botToken, _telegramBotSunscribersFile);
            if(Bot == null)
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
        AppointmentInfo info = new()
        {
            Location = new LocationInfo()
            {
                Service = "PADRON CP - Periodista Azzati",
                Center = "PADRON Periodista Azzati 2",
            },
            PersonInfo = new PersonInfo()
            {
                Name = "Name",
                Surname = "Surname",
                DocumentType = "Pasaporte",
                Document = "761234566",
                PhoneNumber = "681123456",
                Email = "email@email.com"
            }
        };

        List<AppointmentInfo> infos = new()
        {
            info,
            info with
            {
                PersonInfo = new PersonInfo()
                {
                    Name = "Name2",
                    Surname = "Surname2",
                    DocumentType = "Pasaporte",
                    Document = "761234567",
                    PhoneNumber = "681123457",
                    Email = "email2@email.com"
                }
            },
            info with
            {
                PersonInfo = new PersonInfo()
                {
                    Name = "Name3",
                    Surname = "Surname3",
                    DocumentType = "Pasaporte",
                    Document = "761234568",
                    PhoneNumber = "681123458",
                    Email = "email3@email.com"
                }
            }
        };


        AppointmentInfo testInfo = info with
        {
            Location = new LocationInfo()
            {
                Service = "ATENCION ESPECIALIZADA-LICENCIAS, INOCUAS,CONTENEDORES",
                Center = "JUNTA DE DISTRITO ABASTOS",
            }
        };

        AppointmentInfo testInfo2 = info with
        {
            Location = new LocationInfo()
            {
                Service = "ATENCION TRIBUTARIA - CASA CONSISTORIAL",
                Center = "GESTIÓN TRIBUTARIA INTEGRAL",
            },
            PersonInfo = new PersonInfo()
            {
                Name = "Name",
                Surname = "Surname",
                DocumentType = "NIF/NIE",
                Document = "761234567",
                PhoneNumber = "681123457",
                Email = "email2@email.com"
            }
        };


        DateOnly beforeDate = new(2022, 12, 1);

        CreateTelegramBot();
        Programm = new Program(beforeDate, infos);

        var startMessage = $"Bot started. Appointments({Programm._appointmentInfos.Count}): ";
        for(int i = 0; i < Programm._appointmentInfos.Count; ++i)
            startMessage += $"{i}: {Programm._appointmentInfos[i].ToString()} ";
        Bot?.SendMessageToSubscribers(startMessage);
        Programm.Start();
        while(true)
        {

        }
        Console.ReadKey();
        Programm.Stop();
        Bot?.SendMessageToSubscribers("Bot stopped.");
    }

    public record AppointmentWorkInfo : AppointmentInfo
    {
        private readonly DateOnly _beforeDateOriginal;

        public DateTime? ExistingAppointment { get; set; } = null;
        public DateOnly BeforeDate => ExistingAppointment.HasValue ? ExistingAppointment.Value.ToDateOnly() : _beforeDateOriginal;

        public AppointmentWorkInfo(AppointmentInfo appointmentInfo, DateOnly beforeDateOriginal) : base(appointmentInfo)
        {
            _beforeDateOriginal = beforeDateOriginal;
        }
    }

    private List<AppointmentWorkInfo> _appointmentInfos = new List<AppointmentWorkInfo>();

    public AppointmentWorkInfo? CurrentInfo { get; private set; }
    //private readonly DateOnly _beforeDateOriginal = new(2022, 6, 13);

    private Appointments? _appointments;
    private AppointmentCreator? _creator;

    private readonly int _delayInSeconds = 1;
    private System.Timers.Timer? _timer = null;
    public bool Running => _timer != null;

    //public DateTime? ExistingAppointment { get; private set; } = null;

    //private DateOnly BeforeDate => ExistingAppointment.HasValue ? ExistingAppointment.Value.ToDateOnly() : _beforeDateOriginal;

    private readonly BetterChromeDriver _driver;

    public Program(DateOnly beforeDate, AppointmentInfo info) :
        this(new List<AppointmentWorkInfo>() { new AppointmentWorkInfo(info, beforeDate) })
    {
    }

    public Program(DateOnly beforeDate, IEnumerable<AppointmentInfo> appoinmentInfos) :
        this(appoinmentInfos.Select(appoinmentInfo => new AppointmentWorkInfo(appoinmentInfo, beforeDate)))
    {
    }

    public Program(DateOnly beforeDate, params AppointmentInfo[] appoinmentInfos) :
        this(appoinmentInfos.Select(appoinmentInfo => new AppointmentWorkInfo(appoinmentInfo, beforeDate)))
    {
        if(appoinmentInfos.Length == 0)
            throw new ArgumentException(nameof(appoinmentInfos) + " can't be empty");
    }

    public Program(IEnumerable<AppointmentWorkInfo> appoinmentInfos)
    {
        _logger.Log($"Program created. Appointment infos:");
        _appointmentInfos = new List<AppointmentWorkInfo>();

        foreach(var appoinmentInfo in appoinmentInfos)
        {
            _appointmentInfos.Add(appoinmentInfo);
            _logger.Log($"Appointment info: '{appoinmentInfo}'.");
        }

        _driver = new BetterChromeDriver();
    }

    private bool TryCreateBetterAppointment(AppointmentWorkInfo appointmentInfo)
    {
        _logger.StartMethod(appointmentInfo);
        CurrentInfo = appointmentInfo;
        try
        {
            DateTime? oldAppointment = _appointments!.GetAppointment(appointmentInfo.PersonInfo.Document, appointmentInfo.Location)?.DateTime;
            if(oldAppointment == null && appointmentInfo.ExistingAppointment != null)
            {
                Bot?.SendMessageToSubscribers("Warning: Existing appointment was removed since last check.");
                _logger.LogWarning("Existing appointment was removed since last check");
            }
            appointmentInfo.ExistingAppointment = oldAppointment;

            DateOnly? avaliableDate = _creator!.GetFirstAvaliableDate(appointmentInfo.Location, appointmentInfo.BeforeDate);
            if(avaliableDate is null)
            {
                //_bot?.SendMessageToSubscribers("Better date not found.");
                _logger.StopMethod(true, "Better date not found");
                return true;
            }
            else
            {
                _logger.Log($"Found avaliable date {avaliableDate}");
                Bot?.SendMessageToSubscribers($"Found avaliable date {avaliableDate} for {appointmentInfo}.");

                if(appointmentInfo.ExistingAppointment.HasValue)
                {
                    bool appointmentRemoved = _appointments!.TryRemoveAppointment(appointmentInfo.PersonInfo.Document, appointmentInfo.Location);
                    if(appointmentRemoved == false)
                    {
                        Bot?.SendMessageToSubscribers($"Error: Appointment {appointmentInfo} was not removed. But it exists.");
                        _logger.StopMethod(false, "Appointment was not removed. But it exists");
                        return false;
                    }
                }

                bool created = _creator.TrySelectDateTime(avaliableDate.Value, out DateTime createdTime);
                if(created)
                {
                    _creator.FillPersonInfo(appointmentInfo.PersonInfo);
                    created = _creator.TrySubmit();
                }

                if(created)
                {
                    DateTime? createdAppointmentDate = _appointments!.GetAppointment(appointmentInfo.PersonInfo.Document, appointmentInfo.Location)?.DateTime;
                    created = createdAppointmentDate == createdTime;
                }
                if(created)
                {
                    appointmentInfo.ExistingAppointment = createdTime;
                    Bot?.SendMessageToSubscribers($"New appointment created! Date: {createdTime}. Info : {appointmentInfo}'.");
                    _logger.StopMethod(true, "----Created new appointment----", createdTime, appointmentInfo);
                    return true;
                }
                else
                {
                    Bot?.SendMessageToSubscribers($"Error: Appointment {appointmentInfo} was not created but time found. Found time: {avaliableDate}.");
                    _logger.StopMethod(false, "----Appointment was not created but time found----", avaliableDate);
                    return false;
                }
            }
        }
        catch(Exception ex)
        {
            Bot?.SendMessageToSubscribers($"Caught exception: {ex}");
            _logger.LogError(ex.ToString());
            _driver.Quit();
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
                bool doneFine = true;
                foreach(var appointmentInfo in _appointmentInfos)
                {
                    doneFine = TryCreateBetterAppointment(appointmentInfo);
                    if(doneFine == false)
                    {
                        needToBeRestartedImmidiately = true;
                        break;
                    }
                }
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
