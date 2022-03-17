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
        new Program().Start();
        while(true)
        {
            Console.ReadKey();
            return;
        }
    }
    public static readonly string LogPath = DirectoryExt.ProjectDirectory!.Parent!.FullName + "\\logs\\log.log";
    private ClassLogger _logger = new ClassLogger(nameof(Program));

    //private string _service = "ATENCION ESPECIALIZADA-LICENCIAS, INOCUAS,CONTENEDORES";
    //private string _center = "JUNTA DE DISTRITO ABASTOS";

    private static readonly string _service = "PADRON CP - Periodista Azzati";
    private static readonly string _center = "PADRON Periodista Azzati 2";
    private DateOnly _beforeDate = new DateOnly(2022, 6, 13);
    private string _name = "Name";
    private string _surname = "Surname";
    private string _documentType = "Pasaporte";
    private string _document = "761234567";
    private string _phoneNumber = "681123456";
    private string _email = "email@email.com";

    private Appointments? _appointments;
    private AppointmentCreator? _creator;

    private int _delayInSeconds = 60;
    private System.Timers.Timer? _timer = null;
    public bool Running => _timer != null;

    private string _telegramBotSunscribersFile = DirectoryExt.ProjectDirectory!.Parent!.FullName + "\\botSubs.txt";
    private string _telegramBotTokenFile = DirectoryExt.ProjectDirectory!.Parent!.FullName + "\\telegramBotToken.txt";

    private TelegramBot? _bot;

    private bool TryCreateBetterAppointment()
    {
        _logger.StartMethod();
        try
        {
            bool hasOldAppointment = LookForAlreadyCreatedAppointment();
            _logger.Log("Trying to create better appointment");

            DateOnly? avaliableDate = GetFirstAvaliableDate();
            if(avaliableDate != null)
            {
                _bot?.SendMessageToSubscribers($"Found avaliable date {avaliableDate}");
                if(hasOldAppointment && RemoveAppointmentByPoint() == false)
                {
                    _bot?.SendMessageToSubscribers("Error: Appointment was not removed. But it exists.");
                    _logger.StopMethod(false, "Appointment was not removed. But it exists");
                    return false;
                }
                bool created = CreateAppointment(out DateTime createdTime);
                if(created)
                {
                    _bot?.SendMessageToSubscribers($"New appointment created! Date: {createdTime}.");
                    _beforeDate = createdTime.ToDateOnly();
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
            _logger.StopMethod(true, "Date not found");
            return true;
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

    private bool LookForAlreadyCreatedAppointment()
    {
        _logger.StartMethod(_beforeDate);
        DateTime? createdAppointmentDateTime = GetAppointmentDateTime();
        if(createdAppointmentDateTime != null)
        {
            DateOnly createdAppointmentDate = createdAppointmentDateTime.Value.ToDateOnly();
            if(createdAppointmentDate < _beforeDate)
            {
                _beforeDate = createdAppointmentDate;
                _logger.StopMethod(true, $"Found already existing appointment with date {_beforeDate}");
                return true;
            }
            else
            {
                _logger.StopMethod(true, $"Old appointment before date {_beforeDate} was not found. Found appointment date: {createdAppointmentDate}");
                return true;
            }
        }
        else
        {
            _logger.StopMethod(false, "Old appointment was not found");
            return false;
        }
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

    private DateTime? GetAppointmentDateTime()
    {
        _logger.StartMethod();

        _appointments!.Open();
        Appointment? appointment = _appointments.GetAppointment(_service, _center);
        DateTime? result = appointment?.DateTime;
        _appointments.Close();

        _logger.StopMethod(result);
        return result;
    }

    private bool HasAppointment()
    {
        _logger.StartMethod();

        _appointments!.Open();
        bool result = _appointments.HasAppointment(_service, _center);
        _appointments.Close();

        _logger.StopMethod(result);
        return result;
    }

    private bool RemoveAppointmentByPoint()
    {
        _logger.StartMethod();

        _appointments!.Open();
        bool result = _appointments.TryRemoveAppointment(_service, _center);
        _appointments.Close();

        _logger.StopMethod(result);
        return result;
    }

    private DateOnly? GetFirstAvaliableDate()
    {
        _logger.StartMethod();

        _creator!.Open();
        DateOnly? result = _creator.GetFirstAvaliableDate(_service, _center, _beforeDate);
        _creator.Close();

        _logger.StopMethod(result);
        return result;
    }

    private bool CreateAppointment(out DateTime createdDateTime)
    {
        _logger.StartMethod();

        _creator!.Open();
        bool result =_creator.CreateAppointment(_service, _center, _beforeDate, _name, _surname, _documentType, _document, _phoneNumber, _email, out createdDateTime);
        _creator.Close();

        _logger.StopMethod(result);
        return result;
    }
}
