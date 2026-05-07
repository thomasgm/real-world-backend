using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyRoute.Mvc.Data;
using SkyRoute.Mvc.Models;

namespace SkyRoute.Mvc.Controllers;

public class FlightsController : Controller
{
    private readonly SkyDbContext _context;

    public FlightsController(SkyDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var flights = await _context.Flights
            .Include(f => f.OriginAirport)
            .Include(f => f.DestinationAirport)
            .Include(f => f.Airline)
            .ToListAsync();
        return View(flights);
    }

    // 1. Revenue by Airline
    public async Task<IActionResult> AirlineRevenue()
    {
        var revenue = await _context.Tickets
            .Include(t => t.Flight)
            .ThenInclude(f => f.Airline)
            .GroupBy(t => t.Flight.Airline.Name)
            .Select(g => new AirlineRevenueViewModel
            {
                AirlineName = g.Key,
                TotalRevenue = g.Sum(t => t.Price),
                TicketsSold = g.Count()
            })
            .OrderByDescending(r => r.TotalRevenue)
            .ToListAsync();

        return View(revenue);
    }

    // 2. Popular Routes
    public async Task<IActionResult> PopularRoutes()
    {
        var routes = await _context.Flights
            .Include(f => f.OriginAirport)
            .Include(f => f.DestinationAirport)
            .GroupBy(f => new { f.OriginAirport.Name, DestName = f.DestinationAirport.Name })
            .Select(g => new RouteViewModel
            {
                Origin = g.Key.Name,
                Destination = g.Key.DestName,
                FlightCount = g.Count()
            })
            .OrderByDescending(r => r.FlightCount)
            .ToListAsync();

        return View(routes);
    }
}

public class AirlineRevenueViewModel
{
    public string AirlineName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TicketsSold { get; set; }
}

public class RouteViewModel
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int FlightCount { get; set; }
}
