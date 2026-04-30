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
    /*--Q2 Find the top 3 highest-paid 'All-rounders' across all teams: 

SELECT TOP 3 Player, Team, Price_in_cr
FROM IPLPlayers
WHERE Role = 'All-rounder'
ORDER BY Price_in_cr DESC*/
    public async Task<IActionResult> TopAllRounders()
    {
        var topAllRounders = await _context.Players
            .Where(p => p.Role == "All-rounder")
            .OrderByDescending(p => p.Price_in_cr)
            .Take(3)
            .Select(p => new TopAllRoundersViewModel
            {
                PlayerName = p.PlayerName,
                Team = p.Team,
                Price_in_cr = p.Price_in_cr
            })
            .ToListAsync();

        return View(topAllRounders);
    }

/*--Q3 Find the highest-priced player in each team:


WITH CTE_MP AS(
	SELECT Team, Max(Price_in_cr) as MaxPrice
	FROM IPLPlayers
	GROUP BY Team
)
SELECT i.Team, i.Player, c.MaxPrice
FROM IPLPlayers i
JOIN CTE_MP c ON i.Team = c.Team
WHERE i.Price_in_cr = c.MaxPrice*/
    public async Task<IActionResult> HighestPricedPerTeam()
    {
        var maxPricesByTeam = _context.Players
            .GroupBy(p => p.Team)
            .Select(g => new
            {
                Team = g.Key,
                MaxPrice = g.Max(p => p.Price_in_cr)
            });

        var highestPriced = await _context.Players
            .Join(
                maxPricesByTeam,
                player => new { player.Team, player.Price_in_cr },
                max => new { max.Team, Price_in_cr = max.MaxPrice },
                (player, max) => new HighestPricedPerTeamViewModel
                {
                    Team = player.Team,
                    PlayerName = player.PlayerName,
                    Price_in_cr = player.Price_in_cr
                })
            .ToListAsync();

        return View(highestPriced);
    }
/*--Q4 Rank players by their price within each team and list the top 2 for every team:

WITH RankedPlayers AS (
SELECT Player, Team, Price_in_cr,
ROW_NUMBER() OVER (PARTITION BY Team ORDER BY Price_in_cr DESC) AS RankWithinTeam
FROM IPLPlayers
)
SELECT Player, Team, Price_in_cr,RankWithinTeam
FROM RankedPlayers
WHERE RankWithinTeam <=2*/
    public async Task<IActionResult> Top2PerTeam()
    {
        var rankedPlayers = (await _context.Players
            .GroupBy(p => p.Team)
            .ToListAsync())
            .SelectMany(g => g.OrderByDescending(p => p.Price_in_cr)
                .Take(2)
                .Select((p, index) => new Top2PerTeamViewModel
                {
                    PlayerName = p.PlayerName,
                    Team = p.Team,
                    Price_in_cr = p.Price_in_cr,
                    RankWithinTeam = index + 1,
                    Role = p.Role
                }))
            .ToList();

        return View(rankedPlayers);
    }
/*
--Q5 Find the most expensive player from each team, along with the second-most expensive player's name and price:

WITH RankedPlayers AS (
	SELECT Player, Team, Price_in_cr,
	ROW_NUMBER() OVER (PARTITION BY Team ORDER BY Price_in_cr DESC) AS RankWithinTeam
	FROM IPLPlayers
)
SELECT Team,
	MIN(CASE WHEN RankWithinTeam = 1 THEN Player END) AS MostExpensivePlayer,
	MIN(CASE WHEN RankWithinTeam = 1 THEN Price_in_cr END) AS HighestPrice,
	MIN(CASE WHEN RankWithinTeam = 2 THEN Player END) AS SecondMostExpensivePlayer,
	MIN(CASE WHEN RankWithinTeam = 2 THEN Price_in_cr END) AS SecondHighestPrice
FROM RankedPlayers
GROUP BY Team
*/
    public async Task<IActionResult> MostAndSecondMostExpensivePerTeam()
    {
        var rankedPlayers = (await _context.Players
            .GroupBy(p => p.Team)
            .ToListAsync())
            .Select(g => new
            {
                Team = g.Key,
                Players = g.OrderByDescending(p => p.Price_in_cr).Take(2).ToList()
            })
            .ToList();

        var result = rankedPlayers.Select(g => new MostAndSecondMostExpensivePerTeamViewModel 
        {
            Team = g.Team,
            MostExpensivePlayer = g.Players.FirstOrDefault()?.PlayerName ?? "N/A",
            HighestPrice = g.Players.FirstOrDefault()?.Price_in_cr ?? 0,
            SecondMostExpensivePlayer = g.Players.Skip(1).FirstOrDefault()?.PlayerName ?? "N/A",
            SecondHighestPrice = g.Players.Skip(1).FirstOrDefault()?.Price_in_cr ?? 0
        }).ToList();

        return View(result);
    }
/*
--Q6 Calculate the percentage contribution of each player's price to their team's total spending

SELECT Player, Team, Price_in_cr, 
	CAST(Price_in_cr/ (SUM(Price_in_cr) OVER (PARTITION BY Team)) * 100 AS DECIMAL(10,2)) AS ContributionPercentage
FROM IPLPlayers
ORDER BY ContributionPercentage DESC
*/
    public async Task<IActionResult> ContributionPercentage()
    {
        var allPlayers = await _context.Players.ToListAsync();

        var teamTotals = allPlayers
            .GroupBy(p => p.Team)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Price_in_cr));

        var result = allPlayers.Select(p => new ContributionPercentageViewModel
        {
            PlayerName = p.PlayerName,
            Team = p.Team,
            Price_in_cr = p.Price_in_cr,
            // Evitamos divisão por zero caso o time não tenha gastos
            ContributionPercentage = teamTotals[p.Team] > 0 
                ? (p.Price_in_cr / teamTotals[p.Team]) * 100 
                : 0
        })
        .OrderByDescending(p => p.ContributionPercentage)
        .ToList();

        return View(result);
    }

/*
--Q7 Classify players as 'High', 'Medium', or 'Low' priced based on the following rules:
--High: Price > ₹15 crore
--Medium: Price between ₹5 crore and ₹15 crore
--Low: Price < ₹5 crore
--and find out the number of players in each bracket

WITH CTE_BR AS (
	SELECT Team, Player, Price_in_cr,
			CASE 
				WHEN Price_in_cr > 15 THEN 'High'
				WHEN Price_in_cr BETWEEN 5 AND 15 THEN 'Medium'
				ELSE 'Low'
			END AS PriceCategory
	FROM IPLPlayers
)
SELECT Team, PriceCategory, COUNT(*) AS 'NoOfPlayers'
FROM CTE_BR
GROUP BY Team, PriceCategory
ORDER BY Team, PriceCategory
*/
    public async Task<IActionResult> PriceCategoryDistribution()
    {
        var players = await _context.Players.ToListAsync();

        var categorized = players.Select(p => new
        {
            Team = p.Team,
            PriceCategory = p.Price_in_cr > 15 ? "High" :
                            (p.Price_in_cr >= 5 ? "Medium" : "Low")
        });

        var distribution = categorized
            .GroupBy(c => new { c.Team, c.PriceCategory })
            .Select(g => new PriceCategoryDistributionViewModel
            {
                Team = g.Key.Team,
                PriceCategory = g.Key.PriceCategory,
                PlayerCount = g.Count()
            })
            .OrderBy(d => d.Team)
            .ThenBy(d => d.PriceCategory)
            .ToList();

        return View(distribution);
    }

/*--Q8 Find the average price of Indian players and compare it with overseas players using a subquery:

SELECT * FROM IPLPlayers

SELECT 
	'Indian' AS PlayerType,
		(SELECT AVG(Price_in_cr) 
		FROM IPLPlayers
		WHERE Type LIKE 'Indian%') AS AvgPrice
UNION ALL
SELECT 
	'Overseas' AS PlayerType,
		(SELECT AVG(Price_in_cr) 
		FROM IPLPlayers
		WHERE Type LIKE 'Overseas%') AS AvgPrice*/
    public async Task<IActionResult> TypeComparison()
    {
        var stats = await _context.Players
            .Where(p => p.Type.StartsWith("Indian") || p.Type.StartsWith("Overseas"))
            .GroupBy(p => p.Type.StartsWith("Indian") ? "Indian" : "Overseas")
            .Select(g => new PlayerTypeComparisonViewModel
            {
                PlayerType = g.Key,
                AvgPrice = g.Average(p => p.Price_in_cr)
            })
            .ToListAsync();

        return View(stats);
    }
/*
--Q9 Identify players who earn more than the average price of their team:


SELECT Player, Team, Price_in_cr
FROM IPLPlayers p
WHERE Price_in_cr > (
			SELECT AVG(Price_in_cr)
			FROM IPLPlayers
			WHERE Team = p.Team)
            ORDER BY (Price_in_cr - (SELECT AVG(Price_in_cr) FROM IPLPlayers WHERE Team = p.Team)) DESC;
*/
    public async Task<IActionResult> AboveAveragePlayers()
    {
        var allPlayers = await _context.Players.ToListAsync();

        // 1. Calculamos as médias de cada time primeiro
        var teamAverages = allPlayers
            .GroupBy(p => p.Team)
            .ToDictionary(g => g.Key, g => g.Average(p => p.Price_in_cr));

        // 2. Filtramos os jogadores que ganham acima da média do seu respectivo time
        var result = allPlayers
            .Where(p => p.Price_in_cr > teamAverages[p.Team])
            .Select(p => new AboveAveragePlayerViewModel
            {
                PlayerName = p.PlayerName,
                Team = p.Team,
                Price = p.Price_in_cr,
                TeamAverage = teamAverages[p.Team],
                Surplus = p.Price_in_cr - teamAverages[p.Team]
            })
            .OrderByDescending(p => p.Surplus)
            .ToList();

        return View(result);
    }

/*
--Q10 For each role, find the most expensive player and their price using a correlated subquery

SELECT Player, Team, Role, Price_in_cr
FROM IPLPlayers p
WHERE Price_in_cr = (
						SELECT MAX(Price_in_cr) 
						FROM IPLPlayers
						WHERE Role = p.Role
					)
					Order by Price_in_cr DESC
                    */
    public async Task<IActionResult> TopPlayersByRole()
    {
        var players = await _context.Players.ToListAsync();

        // 1. Agrupamos por Role
        // 2. SelectMany para "achatar" a lista de volta
        var result = players
            .GroupBy(p => p.Role)
            .Select(g => g.OrderByDescending(p => p.Price_in_cr).First())
            .Select(p => new TopPlayerPerRoleViewModel
            {
                PlayerName = p.PlayerName,
                Team = p.Team,
                Role = p.Role,
                Price = p.Price_in_cr
            })
            .OrderByDescending(r => r.Price)
            .ToList();

        return View(result);
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

public class TopAllRoundersViewModel
{
    public string PlayerName { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public decimal Price_in_cr { get; set; }
}
public class HighestPricedPerTeamViewModel
{
    public string Team { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public decimal Price_in_cr { get; set; }
}

public class Top2PerTeamViewModel
{
    public string PlayerName { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public decimal Price_in_cr { get; set; }
    public int RankWithinTeam { get; set; }

    public string Role { get; set; } = string.Empty;
}

public class MostAndSecondMostExpensivePerTeamViewModel
{
    public string Team { get; set; } = string.Empty;
    public string? MostExpensivePlayer { get; set; }
    public decimal? HighestPrice { get; set; }
    public string? SecondMostExpensivePlayer { get; set; }
    public decimal? SecondHighestPrice { get; set; }
}

public class ContributionPercentageViewModel
{
    public string PlayerName { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public decimal Price_in_cr { get; set; }
    public decimal ContributionPercentage { get; set; }
}

public class PriceCategoryDistributionViewModel
{
    public string Team { get; set; } = string.Empty;
    public string PriceCategory { get; set; } = string.Empty;
    public int PlayerCount { get; set; }
}

public class PlayerTypeComparisonViewModel
{
    public string PlayerType { get; set; } = string.Empty;
    public decimal AvgPrice { get; set; }
}

public class AboveAveragePlayerViewModel
{
    public string PlayerName { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal TeamAverage { get; set; }
    public decimal Surplus { get; set; } // O quanto ele ganha acima da média
}


public class TopPlayerPerRoleViewModel
{
    public string PlayerName { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
