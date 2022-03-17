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
    public static void Main()
    {
        Program program = new();
        program.Start();
        Console.ReadKey();
        program.Stop();
    }
    public static readonly string LogPath = DirectoryExt.ProjectDirectory!.Parent!.FullName + "\\logs\\log.log";
    private readonly ClassLogger _logger = new(nameof(Program));

    //private string _service = "ATENCION ESPECIALIZADA-LICENCIAS, INOCUAS,CONTENEDORES";
    //private string _center = "JUNTA DE DISTRITO ABASTOS";

    private static readonly string _service = "PADRON CP - Periodista Azzati";
    private static readonly string _center = "PADRON Periodista Azzati 2";
    private readonly DateOnly _beforeDateOriginal = new(2022, 6, 13);
    private readonly string _name = "Name";
    private readonly string _surname = "Surname";
    private readonly string _documentType = "Pasaporte";
    private readonly string _document = "761234567";
    private readonly string _phoneNumber = "681123456";
    private readonly string _email = "email@email.com";

    private Appointments? _appointments;
    private AppointmentCreator? _creator;

    private readonly int _delayInSeconds = 60;
    private System.Timers.Timer? _timer = null;
    public bool Running => _timer != null;

    public static DateTime? ExistingAppointment { get; private set; } = null;

    private DateOnly BeforeDate => ExistingAppointment.HasValue ? ExistingAppointment.Value.ToDateOnly() : _beforeDateOriginal;

    private readonly string _telegramBotSunscribersFile = DirectoryExt.ProjectDirectory!.Parent!.FullName + "\\botSubs.txt";
    private readonly string _telegramBotTokenFile = DirectoryExt.ProjectDirectory!.Parent!.FullName + "\\telegramBotToken.txt";

    private TelegramBot? _bot;

    private bool TryCreateBetterAppointment()
    {
        _logger.StartMethod();
        try
        {
            _logger.Log("Trying to create better appointment");
            bool hasOldAppointment = LookForAlreadyCreatedAppointment(true, false);
            if(hasOldAppointment == false)
                _appointments!.Close();

            DateOnly? avaliableDate = GetFirstAvaliableDate(true, false);
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
                if(hasOldAppointment && RemoveAppointmentByPoint(false, true) == false)
                {
                    _bot?.SendMessageToSubscribers("Error: Appointment was not removed. But it exists.");
                    _logger.StopMethod(false, "Appointment was not removed. But it exists");
                    return false;
                }
                bool created = CreateAppointmentByExactDate(avaliableDate.Value, out DateTime createdTime, false, true);
                if(created)
                {
                    ExistingAppointment = createdTime;
                    _bot?.SendMessageToSubscribers($"New appointment created! Date: {createdTime}.");
                    _logger.StopMethod(true, "----Created new appointment----", createdTime);
                    return true;
                }
                else
                {
                    _bot?.SendMessageToSubscribers($"Error: Appointment was not created but time found. Found time: {avaliableDate}.");
                    _logger.StopMethod(true, "----Appointment was not created but time found----", avaliableDate);
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

        _bot?.SendMessageToSubscribers("Bot stopped.");

        _logger.StopMethod();
    }

    private bool LookForAlreadyCreatedAppointment(bool reOpen = true, bool close = true)
    {
        _logger.StartMethod(BeforeDate);
        DateTime? existingAppointmentDateTime = GetAppointmentDateTime(reOpen, close);

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
                _bot?.SendCurrentAppointmentInfoToSubscribers();
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

        _bot?.SendMessageToSubscribers("Bot started.");

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

    private DateTime? GetAppointmentDateTime(bool reOpen = true, bool close = true)
    {
        _logger.StartMethod();

        if(reOpen)
            _appointments!.Open();
        Appointment? appointment = _appointments!.GetAppointment(_service, _center);
        DateTime? result = appointment?.DateTime;
        if(close)
            _appointments.Close();

        _logger.StopMethod(result);
        return result;
    }

    private bool HasAppointment(bool reOpen = true, bool close = true)
    {
        _logger.StartMethod();

        if(reOpen)
            _appointments!.Open();
        bool result = _appointments!.HasAppointment(_service, _center);
        if(close)
            _appointments.Close();

        _logger.StopMethod(result);
        return result;
    }

    private bool RemoveAppointmentByPoint(bool reOpen = true, bool close = true)
    {
        _logger.StartMethod();

        if(reOpen)
            _appointments!.Open();
        bool result = _appointments!.TryRemoveAppointment(_service, _center);
        if(close)
            _appointments.Close();

        _logger.StopMethod(result);
        return result;
    }

    private DateOnly? GetFirstAvaliableDate(bool reOpen = true, bool close = true)
    {
        _logger.StartMethod();

        if(reOpen)
            _creator!.Open();

        DateOnly? result = _creator!.GetFirstAvaliableDate(_service, _center, BeforeDate);
        if(close)
            _creator.Close();

        _logger.StopMethod(result);
        return result;
    }

    private bool CreateAppointment(out DateTime createdDateTime, bool reOpen = true, bool close = true)
    {
        _logger.StartMethod();

        if(reOpen)
            _creator!.Open();
        bool result =_creator!.CreateAppointment(_service, _center, BeforeDate, _name, _surname, _documentType, _document, _phoneNumber, _email, out createdDateTime);
        if(close)
            _creator.Close();

        _logger.StopMethod(result);
        return result;
    }

    private bool CreateAppointmentByExactDate(DateOnly exactDate, out DateTime createdDateTime, bool reOpen = true, bool close = true)
    {
        _logger.StartMethod();

        if(reOpen)
            _creator!.Open();
        bool result = _creator!.CreateAppointmentByExactDate(_service, _center, exactDate, _name, _surname, _documentType, _document, _phoneNumber, _email, out createdDateTime);
        if(close)
            _creator.Close();

        _logger.StopMethod(result);
        return result;
    }
}
