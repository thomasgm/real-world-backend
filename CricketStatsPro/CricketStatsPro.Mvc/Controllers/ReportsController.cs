using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CricketStatsPro.Mvc.Data;
using CricketStatsPro.Mvc.Models;

namespace CricketStatsPro.Mvc.Controllers;

public class ReportsController : Controller
{
    private readonly T20DbContext _context;

    public ReportsController(T20DbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    // Q1: Matches between two teams in 2024
    /*
SELECT * 
FROM T20I
WHERE ((Team1 = 'South Africa' AND Team2 = 'India') OR (Team2 = 'South Africa' AND Team1 = 'India'))
AND YEAR(MatchDate) = 2024
    */
    public async Task<IActionResult> Q1Matches(string team1 = "India", string team2 = "South Africa", int year = 2024)
    {
        var matches = await _context.Matches
            .AsNoTracking()
            .Where(m => ((m.Team1 == team1 && m.Team2 == team2) || (m.Team2 == team1 && m.Team1 == team2))
                        && m.MatchDate.Year == year)
            .ToListAsync();
        
        ViewBag.Team1 = team1;
        ViewBag.Team2 = team2;
        ViewBag.Year = year;
        return View(matches);
    }

    // Q2: Team with highest wins in 2024
    /*
    SELECT TOP 1 Winner, COUNT(*) AS 'Number of Wins'
FROM T20I
WHERE YEAR(MatchDate) = 2024
GROUP BY Winner
ORDER BY 'Number of Wins' DESC
    */
    public async Task<IActionResult> Q2TopWins(int year = 2024)
    {
        var topTeam = await _context.Matches
            .Where(m => m.MatchDate.Year == year && m.Winner != "tied" && m.Winner != "no result")
            .GroupBy(m => m.Winner)
            .Select(g => new TeamWinsRank
            {
                Team = g.Key,
                Wins = g.Count(),
                Year = year
            })
            .OrderByDescending(t => t.Wins)
            .FirstOrDefaultAsync();

        return View(topTeam);
    }

    // Q3: Rank teams based on wins in 2024
    /*
    SELECT Winner, COUNT(*) AS 'Number of Wins',
	DENSE_RANK() OVER(ORDER BY COUNT(*) DESC) AS Rank_Assigned
FROM T20I
WHERE YEAR(MatchDate) = 2024 AND Winner NOT IN('tied', 'no result')
GROUP BY Winner
*/
    public async Task<IActionResult> Q3RankWins(int year = 2024)
    {
        var teams = await _context.Matches
            .Where(m => m.MatchDate.Year == year && m.Winner != "tied" && m.Winner != "no result")
            .GroupBy(m => m.Winner)
            .Select(g => new
            {
                Team = g.Key,
                Wins = g.Count(),
                Year = year
            })
            .OrderByDescending(t => t.Wins)
            .ToListAsync();

        var rankedList = new List<TeamWinsRank>();
        int rank = 1;
        for (int i = 0; i < teams.Count; i++)
        {
            if (i > 0 && teams[i].Wins < teams[i - 1].Wins)
            {
                rank = i + 1; // Dense rank behavior can vary, this is standard rank. For dense rank: rank++ only if diff.
            }
            rankedList.Add(new TeamWinsRank { Team = teams[i].Team, Wins = teams[i].Wins, Rank = rank});
        }
        ViewBag.Year = year;

        return View(rankedList);
    }

    // Q4: Highest average margin (runs)
    /*
    SELECT TOP 1 Winner, AVG(CAST(SUBSTRING(Margin, 1, CHARINDEX(' ', Margin) - 1) AS INT)) AS Avg_Margin
FROM T20I
WHERE Margin LIKE '%runs'
GROUP BY Winner
ORDER BY Avg_Margin DESC
    */
    public async Task<IActionResult> Q4AvgMarginRuns()
    {
        var matches = await _context.Matches
            .AsNoTracking()
            .Where(m => m.Margin.EndsWith("runs"))
            .ToListAsync();

        var result = matches
            .GroupBy(m => m.Winner)
            .Select(g => new AverageMarginResult
            {
                Team = g.Key,
                AverageMargin = (int)g.Average(m => 
                {
                    var parts = m.Margin.Split(' ');
                    return int.TryParse(parts[0], out int val) ? val : 0;
                })
            })
            .OrderByDescending(x => x.AverageMargin)
            .FirstOrDefault();

        return View(result);
    }

    // Q4.1: Highest average margin (wickets)
    /*
    SELECT TOP 1 Winner, AVG(CAST(SUBSTRING(Margin, 1, CHARINDEX(' ', Margin) - 1) AS INT)) AS Avg_Margin
FROM T20I
WHERE Margin LIKE '%wickets'
GROUP BY Winner
ORDER BY Avg_Margin DESC*/
    public async Task<IActionResult> Q4_1AvgMarginWickets()
    {
        var matches = await _context.Matches
            .Where(m => m.Margin.EndsWith("wickets"))
            .ToListAsync();

        var result = matches
            .GroupBy(m => m.Winner)
            .Select(g => new AverageMarginResult
            {
                Team = g.Key,
                AverageMargin = (int)g.Average(m => 
                {
                    var parts = m.Margin.Split(' ');
                    return int.TryParse(parts[0], out int val) ? val : 0;
                })
            })
            .OrderByDescending(x => x.AverageMargin)
            .FirstOrDefault();

        return View(result);
    }

    // Q5: Matches where margin > average margin (runs)
    /*
    WITH CTE_AvgMargin AS(
	SELECT AVG(CAST(SUBSTRING(Margin, 1, CHARINDEX(' ', Margin) - 1) AS INT)) AS Avg_OverAllMargin
	FROM T20I
	WHERE Margin LIKE '%runs'
)
SELECT T.Team1, T.Team2, T.Winner, T.Margin
FROM T20I T
LEFT JOIN CTE_AvgMargin A ON 1 = 1
WHERE T.Margin LIKE '%runs'
AND CAST(SUBSTRING(Margin, 1, CHARINDEX(' ', Margin) - 1) AS INT) > A.Avg_OverAllMargin
    */
    public async Task<IActionResult> Q5AboveAvgMargin()
    {
        var matches = await _context.Matches
            .AsNoTracking()
            .Where(m => m.Margin.EndsWith("runs"))
            .ToListAsync();

        if (!matches.Any()) return View(new List<T20Match>());

        var avgMargin = matches.Average(m => 
        {
            var parts = m.Margin.Split(' ');
            return int.TryParse(parts[0], out int val) ? val : 0;
        });

        var aboveAvgMatches = matches.Where(m => 
        {
            var parts = m.Margin.Split(' ');
            int marginVal = int.TryParse(parts[0], out int val) ? val : 0;
            return marginVal > avgMargin;
        }).ToList();

        ViewBag.AverageMargin = Math.Round(avgMargin, 0);
        return View(aboveAvgMatches);
    }

    // Q6: Most wins when chasing
/*
SELECT * 
FROM T20I

SELECT Winner, WinWhileChasing
FROM (
		SELECT Winner, COUNT(*) AS WinWhileChasing,
			RANK() OVER( ORDER BY COUNT(*) DESC) AS rk
		FROM T20I
		WHERE Margin LIKE '%wickets'
		AND Winner NOT IN ('tied', 'no result')
		GROUP BY Winner
) t
WHERE rk = 1
*/
    public async Task<IActionResult> Q6MostWinsChasing()
    {
        var matches = await _context.Matches
            .AsNoTracking()
            .Where(m => m.Margin.EndsWith("wickets") && m.Winner != "tied" && m.Winner != "no result")
            .ToListAsync();

        var result = matches
            .GroupBy(m => m.Winner)
            .Select(g => new ChasingWinsResult
            {
                Team = g.Key,
                WinsWhileChasing = g.Count()
            })
            .OrderByDescending(x => x.WinsWhileChasing)
            .ToList();

        // Assign Rank
        int rank = 1;
        for (int i = 0; i < result.Count; i++)
        {
            if (i > 0 && result[i].WinsWhileChasing < result[i - 1].WinsWhileChasing) rank = i + 1;
            result[i].Rank = rank;
        }

        var topChasers = result.Where(r => r.Rank == 1).ToList();
        return View(topChasers);
    }

    // Q7: Head to head aggregate
    /*
    DECLARE @TeamA VARCHAR(25) = 'India';
DECLARE @TeamB VARCHAR(25) = 'South Africa';

SELECT Winner, Count(*) AS Matches
FROM T20I
WHERE (Team1 = @TeamA AND Team2= @TeamB) OR (Team1 = @TeamB AND Team2= @TeamA)
GROUP BY Winner
    */
    public async Task<IActionResult> Q7HeadToHead(string teamA = "India", string teamB = "South Africa")
    {
        var matches = await _context.Matches
            .Where(m => (m.Team1 == teamA && m.Team2 == teamB) || (m.Team1 == teamB && m.Team2 == teamA))
            .GroupBy(m => m.Winner)
            .Select(g => new HeadToHeadAggregate
            {
                TeamA = teamA,
                TeamB = teamB,
                Winner = g.Key,
                MatchesWon = g.Count()
            })
            .ToListAsync();

        ViewBag.TeamA = teamA;
        ViewBag.TeamB = teamB;
        return View(matches);
    }

    // Q8: Month in 2024 with most matches
/*
SELECT *
FROM T20I

SELECT YEAR(MatchDate) AS YearPlayed,
	   --Month(MatchDate) AS MonthNumber,
	   DATENAME(MONTH, MatchDate) AS MonthName,
	   COUNT(*) AS MatchesPlayed
FROM T20I
WHERE YEAR(MatchDate) = 2024
GROUP BY YEAR(MatchDate), Month(MatchDate), DATENAME(MONTH, MatchDate)
ORDER BY MatchesPlayed DESC
*/
    public async Task<IActionResult> Q8MonthMostMatches(int year = 2024)
    {
        var result = await _context.Matches
            .Where(m => m.MatchDate.Year == year)
            .GroupBy(m => m.MatchDate.Month) 
            .Select(g => new MonthMatchesResult
            {
                MonthName = System.Globalization.CultureInfo.InvariantCulture
                            .DateTimeFormat.GetAbbreviatedMonthName(g.Key),
                Year = year,
                MatchesPlayed = g.Count()
            })
            .OrderByDescending(x => x.MatchesPlayed)
            .ToListAsync();
        
        ViewBag.Year = year;

        return View(result);
    }


    // Q9: Matches played and win percentage in 2024
    /*
    SELECT *
FROM T20I;

WITH CTE_MatchesPlayed AS (
	SELECT Team, COUNT(*) AS MatchesPlayed
	FROM (
			SELECT Team1 AS Team
			FROM T20I
			WHERE YEAR(MatchDate) = 2024
			UNION ALL
			SELECT Team2 AS Team
			FROM T20I
			WHERE YEAR(MatchDate) = 2024
		 ) t
	GROUP BY Team
	),
CTE_Wins AS (
	SELECT Winner AS Team, COUNT(*) AS Wins
	FROM  T20I
	WHERE YEAR(MatchDate) = 2024 AND Winner NOT IN ('tied', 'no result')
	GROUP BY Winner
)
SELECT 
		m.Team, m.MatchesPlayed, ISNULL(w.Wins, 0) AS Wins,
		CAST(ISNULL(w.Wins, 0) * 100.0/m.MatchesPlayed AS DECIMAL(5,2)) AS WinPercentage
From CTE_MatchesPlayed m
LEFT JOIN CTE_Wins w
ON m.Team = w.Team
ORDER BY WinPercentage DESC
    */
    public async Task<IActionResult> Q9WinPercentage(int year = 2024)
    {
        var matches2024 = await _context.Matches
            .AsNoTracking()
            .Where(m => m.MatchDate.Year == year)
            .ToListAsync();

        // Matches played per team
        var played = matches2024.Select(m => m.Team1)
            .Concat(matches2024.Select(m => m.Team2))
            .GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count());

        var wins = matches2024
            .Where(m => m.Winner != "tied" && m.Winner != "no result")
            .GroupBy(m => m.Winner)
            .ToDictionary(g => g.Key, g => g.Count());

        var results = new List<WinPercentageResult>();
        foreach(var team in played)
        {
            int teamWins = wins.ContainsKey(team.Key) ? wins[team.Key] : 0;
            results.Add(new WinPercentageResult
            {
                Team = team.Key,
                MatchesPlayed = team.Value,
                Wins = teamWins,
                WinPercentage = team.Value > 0 ? Math.Round((decimal)teamWins * 100 / team.Value, 2) : 0
            });
        }

        return View(results.OrderByDescending(r => r.WinPercentage).ToList());
    }

    // Q10: Most successful team at each ground
    /*
    SELECT *
FROM T20I

WITH CTE_WinsPerGround AS (
	SELECT Ground, Winner, Wins, RANK() OVER (PARTITION BY Ground ORDER BY Wins DESC) AS rn
	FROM (
			SELECT Ground, Winner, COUNT(*) AS Wins
			FROM T20I 
			WHERE Winner NOT IN ('tied','no result')
			GROUP BY Ground, Winner
		 ) t
)
SELECT Ground, Winner AS MostSuccessful, Wins
FROM CTE_WinsPerGround
WHERE rn = 1
ORDER BY Ground
    */
    public async Task<IActionResult> Q10GroundSuccess()
    {
        var matches = await _context.Matches
            .AsNoTracking()
            .Where(m => m.Winner != "tied" && m.Winner != "no result")
            .ToListAsync();

        var results = matches
            .GroupBy(m => new { m.Ground, m.Winner })
            .Select(g => new { g.Key.Ground, g.Key.Winner, Wins = g.Count() })
            .GroupBy(x => x.Ground)
            .Select(g => 
            {
                var top = g.OrderByDescending(x => x.Wins).First();
                return new GroundSuccessResult
                {
                    Ground = top.Ground,
                    MostSuccessfulTeam = top.Winner,
                    Wins = top.Wins
                };
            })
            .OrderBy(r => r.Ground)
            .ToList();

        return View(results);
    }
}
