using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyRoute.Mvc.Models;

public class Airport
{
    [Key]
    [Required]
    public string AirportID { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string Location { get; set; }

    [Required]
    public string Country { get; set; }
}

public class Airline
{
    [Key]
    public string AirlineID { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Country { get; set; } = string.Empty;
}

public class Flight
{
    [Key]
    public int FlightID { get; set; }
    
    public string Origin { get; set; } = string.Empty;
    [ForeignKey("Origin")]
    public Airport OriginAirport { get; set; } = null!;

    public string Destination { get; set; } = string.Empty;
    [ForeignKey("Destination")]
    public Airport DestinationAirport { get; set; } = null!;

    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }

    public string AirlineID { get; set; } = string.Empty;
    [ForeignKey("AirlineID")]
    public Airline Airline { get; set; } = null!;
}

public class Passenger
{
    [Key]
    public int PassengerID { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DOB { get; set; }
    public string FrequentFlyerStatus { get; set; } = string.Empty;
}

public class Ticket
{
    [Key]
    public int TicketID { get; set; }
    
    public int PassengerID { get; set; }
    [ForeignKey("PassengerID")]
    public Passenger Passenger { get; set; } = null!;

    public int FlightID { get; set; }
    [ForeignKey("FlightID")]
    public Flight Flight { get; set; } = null!;

    public decimal Price { get; set; }
}
