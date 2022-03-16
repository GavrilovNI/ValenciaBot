using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ValenciaBot;

public class Program
{
    public static void Main()
    {
        new Program().Start();
        while(true)
        {
            Console.ReadLine();
            return;
        }
    }

    private static readonly string _service = "PADRON CP - Periodista Azzati";
    private static readonly string _center = "PADRON Periodista Azzati 2";

    //private string _service = "ATENCION ESPECIALIZADA-LICENCIAS, INOCUAS,CONTENEDORES";
    //private string _center = "JUNTA DE DISTRITO ABASTOS";
    private DateTime _beforeDate = new DateTime(2022, 6, 13);
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

    private bool Do()
    {
        try
        {
            Logger.Log("Trying to create better appointment");

            DateTime? avaliableDate = GetFirstAvaliableDate();
            if(avaliableDate != null)
            {
                if(HasAppointment())
                {
                    if(RemoveAppointmentByPoint() == false)
                    {
                        Logger.LogError("Appointment was not removed. but it exists");
                        return false;
                    }
                }
                bool created = CreateAppointment(out DateTime createdTime);
                if(created)
                {
                    _beforeDate = createdTime;
                    Logger.Log("----Created new appointment----");
                }
                return true;

            }

            return true;
        }
        catch(Exception ex)
        {
            Logger.LogError($"{ex.Message} {ex.StackTrace}");
            _creator!.Close();
            _appointments!.Close();
            return false;
        }
    }

    public void Stop()
    {
        if(Running == false)
            return;
        Logger.Log("Stop");

        _timer!.Stop();
        _timer!.Dispose();
        _timer = null;
        _appointments!.Close();
        _creator!.Close();

    }


    public void Start()
    {
        if(Running)
            return;
        Logger.Log("Start");
        _appointments = new Appointments(_document);
        _creator = new AppointmentCreator();

        DateTime? createdAppointmentTime = GetAppointmentDate();
        if(createdAppointmentTime != null)
        {
            if(createdAppointmentTime.Value < _beforeDate)
            {
                _beforeDate = createdAppointmentTime.Value;
                Logger.Log($"Found already existing appointment with date {_beforeDate:dd:MM:yyyy}");
            }
        }

        void TimerTask(object? o, object? a)
        {
            if(_timer != null)
                _timer.Stop();

            bool needToBeRestartedImmidiately = false;
            do
            {
                bool doneFine = Do();
                needToBeRestartedImmidiately = doneFine == false;
                Logger.Log($"needToBeRestartedImmidiately :{needToBeRestartedImmidiately}");
            }
            while(needToBeRestartedImmidiately && _timer != null);

            if(_timer != null)
                _timer.Start();
        }

        _timer = new System.Timers.Timer(_delayInSeconds * 1000);
        _timer.Elapsed += TimerTask;
        _timer.AutoReset = false;

        TimerTask(null, null);
    }

    private DateTime? GetAppointmentDate()
    {
        _appointments!.Open();
        Appointment? appointment = _appointments.GetAppointment(_service, _center);
        DateTime? result = appointment?.Time;
        _appointments.Close();
        return result;
    }

    private bool HasAppointment()
    {
        _appointments!.Open();
        bool has = _appointments.HasAppointment(_service, _center);
        _appointments.Close();
        return has;
    }

    private bool RemoveAppointmentByPoint()
    {
        _appointments!.Open();
        bool removed = _appointments.TryRemoveAppointment(_service, _center);
        _appointments.Close();
        return removed;
    }

    private DateTime? GetFirstAvaliableDate()
    {
        _creator!.Open();
        DateTime? dateTime = _creator.GetFirstAvaliableDate(_service, _center, _beforeDate);
        _creator.Close();

        return dateTime;
    }

    private bool CreateAppointment(out DateTime createdTime)
    {
        _creator!.Open();
        bool created =_creator.CreateAppointment(_service, _center, _beforeDate, _name, _surname, _documentType, _document, _phoneNumber, _email, out createdTime);
        _creator.Close();

        return created;
    }
}
