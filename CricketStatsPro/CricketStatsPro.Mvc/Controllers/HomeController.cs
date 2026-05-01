using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CricketStatsPro.Mvc.Models;
using CricketStatsPro.Mvc.Data;
using Microsoft.EntityFrameworkCore;

namespace CricketStatsPro.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly T20DbContext _context;

    public HomeController(T20DbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalMatches = await _context.Matches.CountAsync();
        
        // Count unique teams
        var teams1 = await _context.Matches.Select(m => m.Team1).Distinct().ToListAsync();
        var teams2 = await _context.Matches.Select(m => m.Team2).Distinct().ToListAsync();
        var totalTeams = teams1.Union(teams2).Distinct().Count();

        // Count unique grounds
        var totalGrounds = await _context.Matches.Select(m => m.Ground).Distinct().CountAsync();

        var model = new HomeDashboardViewModel
        {
            TotalMatches = totalMatches,
            TotalTeams = totalTeams,
            TotalGrounds = totalGrounds
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
