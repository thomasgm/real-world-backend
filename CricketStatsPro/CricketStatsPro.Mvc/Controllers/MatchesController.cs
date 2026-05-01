using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CricketStatsPro.Mvc.Data;
using CricketStatsPro.Mvc.Models;

namespace CricketStatsPro.Mvc.Controllers;

public class MatchesController : Controller
{
    private readonly T20DbContext _context;

    public MatchesController(T20DbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Matches.ToListAsync());
    }

    // 1. Head to Head analysis
    public async Task<IActionResult> HeadToHead(string team1 = "India", string team2 = "South Africa")
    {
        var matches = await _context.Matches
            .Where(m => (m.Team1 == team1 && m.Team2 == team2) || (m.Team1 == team2 && m.Team2 == team1))
            .ToListAsync();

        var stats = new HeadToHeadViewModel
        {
            Team1 = team1,
            Team2 = team2,
            Team1Wins = matches.Count(m => m.Winner == team1),
            Team2Wins = matches.Count(m => m.Winner == team2),
            TotalMatches = matches.Count
        };

        return View(stats);
    }

    // 2. Chasing performance (winning by wickets)
    public async Task<IActionResult> ChasingStats()
    {
        var stats = await _context.Matches
            .Where(m => m.Margin.Contains("wickets"))
            .GroupBy(m => m.Winner)
            .Select(g => new ChasingViewModel
            {
                TeamName = g.Key,
                WinsWhileChasing = g.Count()
            })
            .OrderByDescending(s => s.WinsWhileChasing)
            .ToListAsync();

        return View(stats);
    }

    // 3. Ground popularity
    public async Task<IActionResult> GroundStats()
    {
        var stats = await _context.Matches
            .GroupBy(m => m.Ground)
            .Select(g => new GroundViewModel
            {
                GroundName = g.Key,
                MatchCount = g.Count()
            })
            .OrderByDescending(s => s.MatchCount)
            .ToListAsync();

        return View(stats);
    }
}

public class HeadToHeadViewModel
{
    public string Team1 { get; set; } = string.Empty;
    public string Team2 { get; set; } = string.Empty;
    public int Team1Wins { get; set; }
    public int Team2Wins { get; set; }
    public int TotalMatches { get; set; }
}

public class ChasingViewModel
{
    public string TeamName { get; set; } = string.Empty;
    public int WinsWhileChasing { get; set; }
}

public class GroundViewModel
{
    public string GroundName { get; set; } = string.Empty;
    public int MatchCount { get; set; }
}
