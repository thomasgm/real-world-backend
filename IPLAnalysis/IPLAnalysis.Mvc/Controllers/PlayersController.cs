using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IPLAnalysis.Mvc.Data;
using IPLAnalysis.Mvc.Models;

namespace IPLAnalysis.Mvc.Controllers;

public class PlayersController : Controller
{
    private readonly IplDbContext _context;

    public PlayersController(IplDbContext context)
    {
        _context = context;
    }

    // 1. List all players
    public async Task<IActionResult> Index()
    {
        return View(await _context.Players.ToListAsync());
    }

    // 2. Total spending per team
    public async Task<IActionResult> TeamSpending()
    {
        var spending = await _context.Players
            .GroupBy(p => p.Team)
            .Select(g => new TeamSpendingViewModel
            {
                TeamName = g.Key,
                TotalSpent = g.Sum(p => p.Price_in_cr),
                PlayerCount = g.Count()
            })
            .OrderByDescending(s => s.TotalSpent)
            .ToListAsync();

        return View(spending);
    }

    // 3. Top earners
    public async Task<IActionResult> TopEarners()
    {
        var topPlayers = await _context.Players
            .OrderByDescending(p => p.Price_in_cr)
            .Take(10)
            .ToListAsync();

        return View(topPlayers);
    }

    // 4. Role stats
    public async Task<IActionResult> RoleStats()
    {
        var stats = await _context.Players
            .GroupBy(p => p.Role)
            .Select(g => new RoleStatsViewModel
            {
                Role = g.Key,
                AveragePrice = g.Average(p => p.Price_in_cr),
                PlayerCount = g.Count()
            })
            .ToListAsync();

        return View(stats);
    }
}

public class TeamSpendingViewModel
{
    public string TeamName { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public int PlayerCount { get; set; }
}

public class RoleStatsViewModel
{
    public string Role { get; set; } = string.Empty;
    public decimal AveragePrice { get; set; }
    public int PlayerCount { get; set; }
}
