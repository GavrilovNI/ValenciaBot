using System;
using System.Collections.Generic;
using System.Linq;
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
        //CreateTelegramBot();
        _programs = new Program[]
        {
            new Program("ATENCION ESPECIALIZADA-LICENCIAS, INOCUAS,CONTENEDORES", "JUNTA DE DISTRITO ABASTOS"),
        };
        /*_programs = new Program[]
        {
            new Program("PADRON CP - Periodista Azzati", "PADRON Periodista Azzati 2"),
            new Program("PADRON CP - OAC Tabacalera", "OAC TABACALERA"),
            new Program("PADRON CP - Juntas Municipales", "JUNTA DE DISTRITO ABASTOS"),
            new Program("PADRON CP - Juntas Municipales", "JUNTA DE DISTRITO MARITIMO"),
            new Program("PADRON CP - Juntas Municipales", "JUNTA DE DISTRITO TRANSITS"),
        };*/

        _bot?.SendMessageToSubscribers("Bot started.");
        foreach (Program program in Programs)
            program.Start();
        Console.ReadKey();
        foreach(Program program in Programs)
            program.Stop();
        _bot?.SendMessageToSubscribers("Bot stopped.");
    }

    public string Service => _service;
    public string Center => _center;

    private readonly string _service;
    private readonly string _center;
    private readonly DateOnly _beforeDateOriginal = new(2022, 6, 13);
    private readonly string _name = "Name";
    private readonly string _surname = "Surname";
    private readonly string _documentType = "Pasaporte";
    private readonly string _document = "761234566";
    private readonly string _phoneNumber = "681123456";
    private readonly string _email = "email@email.com";

    private Appointments? _appointments;
    private AppointmentCreator? _creator;

    private readonly int _delayInSeconds = 60;
    private System.Timers.Timer? _timer = null;
    public bool Running => _timer != null;

    public DateTime? ExistingAppointment { get; private set; } = null;

    private DateOnly BeforeDate => ExistingAppointment.HasValue ? ExistingAppointment.Value.ToDateOnly() : _beforeDateOriginal;

    public Program(string service, string center)
    {
        _logger.Log($"Program created. Service: '{service}'. Center: '{center}'");
        _service = service;
        _center = center;
    }

    private bool TryCreateBetterAppointment()
    {
        _logger.StartMethod();
        try
        {
            _logger.Log("Trying to create better appointment");
            _appointments!.Open();
            bool hasOldAppointment = LookForAlreadyCreatedAppointment(_service, _center);
            if(hasOldAppointment == false)
                _appointments!.Close();

            _creator!.Open();
            DateOnly? avaliableDate = _creator.GetFirstAvaliableDate(_service, _center, BeforeDate);
            if(avaliableDate is null)
            {
                _appointments!.Close();
                _creator!.Close();
                //_bot?.SendMessageToSubscribers("Better date not found.");
                _logger.StopMethod(true, "Better date not found");
                return true;
            }
            else
            {
                _bot?.SendMessageToSubscribers($"Found avaliable date {avaliableDate}");
                if(hasOldAppointment)
                {
                    bool appointmentRemoved = _appointments.TryRemoveAppointment(_service, _center);
                    _appointments.Close();
                    if(appointmentRemoved == false)
                    {
                        _bot?.SendMessageToSubscribers("Error: Appointment was not removed. But it exists.");
                        _logger.StopMethod(false, "Appointment was not removed. But it exists");
                        return false;
                    }
                }
                
                bool created = _creator.CreateAppointmentByExactDate(_service, _center, avaliableDate.Value, _name, _surname, _documentType, _document, _phoneNumber, _email, out DateTime createdTime);
                _creator.Close();
                if(created)
                {
                    ExistingAppointment = createdTime;
                    _bot?.SendMessageToSubscribers($"New appointment created! Date: {createdTime}. Document : {_document}. Service: '{_service}'. Center: '{_center}'.");
                    _logger.StopMethod(true, "----Created new appointment----", createdTime, _document, _service, _center);
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
            _creator!.Close();
            _appointments!.Close();
            _logger.StopMethod(false, ex);
            return false;
        }
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

        _logger.StopMethod();
    }

    private bool LookForAlreadyCreatedAppointment(string service, string center)
    {
        _logger.StartMethod(BeforeDate);
        DateTime? existingAppointmentDateTime = _appointments!.GetAppointment(service, center)?.DateTime;

        if(existingAppointmentDateTime is null && ExistingAppointment is not null)
        {
            _bot?.SendMessageToSubscribers("Warning: Existing appointment was removed since last check.");
            _logger.StopMethod(true, $"Existing appointment was removed since last check");
        }

        DateOnly oldBeforeDate = BeforeDate;
        ExistingAppointment = existingAppointmentDateTime;

        if(existingAppointmentDateTime != null)
        {
            DateOnly existingAppointmentDate = existingAppointmentDateTime.Value.ToDateOnly();
            if(existingAppointmentDate < oldBeforeDate)
            {
                _bot?.SendCurrentAppointmentInfoToSubscribers(this);
                _logger.StopMethod(true, $"Found already existing appointment with date {existingAppointmentDateTime}");
            }
            else
            {
                _logger.StopMethod(true, $"Old appointment before date {oldBeforeDate} was not found. Found appointment date: {existingAppointmentDate}");
            }
        }
        else
        {
            _logger.StopMethod(false, "Old appointment was not found");
        }

        return ExistingAppointment is not null;
    }

    public void Start()
    {
        if(Running)
            return;
        _logger.StartMethod();

        _appointments = new Appointments(_document);
        _creator = new AppointmentCreator();

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
}
