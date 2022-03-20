using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValenciaBot;

public record LocationInfo
{
    public string Service { get; init; }
    public string Center { get; init; }

    public LocationInfo(string service = "",
                           string center = "")
    {
        Service = service;
        Center = center;
    }

    public LocationInfo(LocationInfo other)
    {
        Service = other.Service;
        Center = other.Center;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append($"{nameof(Service)}: '{Service}', ");
        sb.Append($"{nameof(Center)}: '{Center}'");

        return sb.ToString();
    }
}

public record PersonInfo
{
    public string Name { get; init; }
    public string Surname { get; init; }
    public string DocumentType { get; init; }
    public string Document { get; init; }
    public string PhoneNumber { get; init; }
    public string Email { get; init; }

    public PersonInfo()
    {
        Name = "";
        Surname = "";
        DocumentType = "";
        Document = "";
        PhoneNumber = "";
        Email = "";
    }

    public PersonInfo(PersonInfo other)
    {
        Name = other.Name;
        Surname = other.Surname;
        DocumentType = other.DocumentType;
        Document = other.Document;
        PhoneNumber = other.PhoneNumber;
        Email = other.Email;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append($"{nameof(Name)}: '{Name}', ");
        sb.Append($"{nameof(Surname)}: '{Surname}', ");
        sb.Append($"{nameof(DocumentType)}: '{DocumentType}', ");
        sb.Append($"{nameof(Document)}: '{Document}', ");
        sb.Append($"{nameof(PhoneNumber)}: '{PhoneNumber}', ");
        sb.Append($"{nameof(Email)}: '{Email}'");

        return sb.ToString();
    }
}

public record AppointmentInfo
{
    public LocationInfo Location { get; init; }
    public PersonInfo PersonInfo { get; init; }

    public AppointmentInfo()
    {
        Location = new LocationInfo();
        PersonInfo = new PersonInfo();
    }

    public AppointmentInfo(AppointmentInfo other)
    {
        Location = new LocationInfo(other.Location);
        PersonInfo = new PersonInfo(other.PersonInfo);
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append($"{Location}, ");
        sb.Append($"{PersonInfo}");

        return sb.ToString();
    }
}
