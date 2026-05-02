using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CricketStatsPro.Mvc.Controllers;
using CricketStatsPro.Mvc.Data;
using CricketStatsPro.Mvc.Models;
using Xunit;

namespace CricketStatsPro.Tests;

public class ReportsControllerTests
{
    /// <summary>Empty DB (no seed data) for edge-case tests.</summary>
    private T20DbContext GetEmptyDbContext() => GetDbContext(seedData: false);

    private T20DbContext GetDbContext(bool seedData = true)
    {
        var databaseName = $"T20TestDb_{Guid.NewGuid():N}";
        var connectionString = $"Server=localhost;Database={databaseName};User Id=sa;Password=Brasil#102030;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<T20DbContext>()
            .UseSqlServer(connectionString)
            .Options;
        var context = new T20DbContext(options);
        context.Database.EnsureCreated(); // Cria tabelas se não existirem
        if (seedData)
        {
            SeedTestData(context); // Popula com dados de teste
        }
        return context;
    }
    private static void SeedTestData(T20DbContext context)
    {
        context.Matches.AddRange(new List<T20Match>
        {
            // India vs South Africa — 2024
            new T20Match { Team1 = "India",        Team2 = "South Africa", Winner = "India",        Margin = "61 runs",   MatchDate = new DateTime(2024, 3, 27), Ground = "Durban" },
            new T20Match { Team1 = "South Africa", Team2 = "India",        Winner = "South Africa", Margin = "3 wickets", MatchDate = new DateTime(2024, 3, 29), Ground = "Gqeberha" },
            new T20Match { Team1 = "India",        Team2 = "South Africa", Winner = "India",        Margin = "135 runs",  MatchDate = new DateTime(2024, 3, 30), Ground = "Johannesburg" },

            // Australia vs England — 2024
            new T20Match { Team1 = "Australia",    Team2 = "England",      Winner = "tied",         Margin = "tied",      MatchDate = new DateTime(2024, 6,  5), Ground = "Melbourne" },
            new T20Match { Team1 = "England",      Team2 = "Australia",    Winner = "no result",    Margin = "no result", MatchDate = new DateTime(2024, 6,  7), Ground = "Melbourne" },
            new T20Match { Team1 = "Australia",    Team2 = "England",      Winner = "Australia",    Margin = "5 wickets", MatchDate = new DateTime(2024, 6, 10), Ground = "Melbourne" },
            new T20Match { Team1 = "Australia",    Team2 = "England",      Winner = "Australia",    Margin = "40 runs",   MatchDate = new DateTime(2024, 6, 12), Ground = "Sydney" },

            // Partida em 2023 (excluída por queries com filtro de ano 2024)
            new T20Match { Team1 = "India",        Team2 = "Australia",    Winner = "India",        Margin = "20 runs",   MatchDate = new DateTime(2023, 9,  1), Ground = "Kolkata" },
        });
        context.SaveChanges();
    }

    /// <summary>Empty database for edge-case tests.</summary>
    private T20DbContext GetSqlDbContext()
    {
        // 1. Gera um nome único para o banco de teste
        string dbName = $"T20TestDB_{Guid.NewGuid()}";
        
        // 2. Define a string de conexão (ajuste conforme seu appsettings)
        string connectionString = $"Server=localhost;Database={dbName};User Id=sa;Password=Brasil#102030;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<T20DbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var context = new T20DbContext(options);

        // 3. Cria o schema do banco fisicamente
        context.Database.EnsureCreated();

        return context;
    }


    // -----------------------------------------------------------------------
    // Q1 — Matches between two teams in a given year
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Q1Matches_ReturnsOnlyMatchesBetweenTeamsInYear()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q1Matches("India", "South Africa", 2024);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<T20Match>>(viewResult.ViewData.Model);
        Assert.Equal(3, model.Count());
        Assert.All(model, m =>
        {
            Assert.Equal(2024, m.MatchDate.Year);
            Assert.True(
                (m.Team1 == "India" && m.Team2 == "South Africa") ||
                (m.Team1 == "South Africa" && m.Team2 == "India"));
        });
    }

    [Fact]
    public async Task Q1Matches_ReturnsEmpty_WhenNoMatchesInYear()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q1Matches("India", "South Africa", 2022);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<T20Match>>(viewResult.ViewData.Model);
        Assert.Empty(model);
    }

    // -----------------------------------------------------------------------
    // Q2 — Team with highest wins in 2024
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Q2TopWins_ReturnsTeamWithMostWinsIn2024()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q2TopWins();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TeamWinsRank>(viewResult.ViewData.Model);
        // India: 2 wins, Australia: 2 wins — both equal, either can be first (impl is non-deterministic on tie)
        Assert.Equal(2, model.Wins);
    }

    [Fact]
    public async Task Q2TopWins_Excludes_TiedAndNoResult_Matches()
    {
        // Only "tied" and "no result" matches — nobody should win
        var ctx = GetEmptyDbContext();
        ctx.Matches.AddRange(
            new T20Match { Team1 = "A", Team2 = "B", Winner = "tied",      Margin = "tied",      MatchDate = new DateTime(2024, 1, 1), Ground = "X" },
            new T20Match { Team1 = "A", Team2 = "B", Winner = "no result", Margin = "no result", MatchDate = new DateTime(2024, 1, 2), Ground = "X" }
        );
        ctx.SaveChanges();
        var controller = new ReportsController(ctx);

        var result = await controller.Q2TopWins();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewData.Model); // FirstOrDefaultAsync returns null
    }

    // -----------------------------------------------------------------------
    // Q3 — Rank teams by wins in 2024 (with tie-breaking)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Q3RankWins_RanksList_IsOrderedDescending()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q3RankWins();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<TeamWinsRank>>(viewResult.ViewData.Model).ToList();
        for (int i = 1; i < model.Count; i++)
            Assert.True(model[i].Wins <= model[i - 1].Wins);
    }

    [Fact]
    public async Task Q3RankWins_AssignsSameRank_WhenTeamsHaveSameWins()
    {
        // Build a DB where India and Australia both have exactly 2 wins in 2024
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q3RankWins();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<TeamWinsRank>>(viewResult.ViewData.Model).ToList();

        var rank1Teams = model.Where(t => t.Wins == 2).ToList();
        // All teams with the same win count must share the same rank value
        var distinctRanks = rank1Teams.Select(t => t.Rank).Distinct().ToList();
        Assert.Single(distinctRanks);
    }

    [Fact]
    public async Task Q3RankWins_Excludes_2023_Matches()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q3RankWins();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<TeamWinsRank>>(viewResult.ViewData.Model).ToList();

        // "India" has 1 extra win in 2023 — it must NOT appear in 2024 ranking totals
        // India's 2024 wins = 2 (not 3)
        var india = model.FirstOrDefault(t => t.Team == "India");
        if (india != null)
            Assert.Equal(2, india.Wins);
    }

    // -----------------------------------------------------------------------
    // Q4 — Highest average margin (runs)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Q4AvgMarginRuns_ParsesRunsAndFindsHighestAverage()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q4AvgMarginRuns();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AverageMarginResult>(viewResult.ViewData.Model);
        // India won by 61 + 135 runs → avg 98. Australia won by 40 runs → avg 40.
        Assert.Equal("India", model.Team);
        // conta (61 + 135 + 20) / 3 = 72
        Assert.Equal(72.0, model.AverageMargin); 
    }

    [Fact]
    public async Task Q4AvgMarginRuns_ReturnsNull_WhenNoRunsMargins()
    {
        var ctx = GetEmptyDbContext();
        ctx.Matches.Add(new T20Match { Team1 = "A", Team2 = "B", Winner = "A", Margin = "5 wickets", MatchDate = DateTime.Now, Ground = "X" });
        ctx.SaveChanges();
        var controller = new ReportsController(ctx);

        var result = await controller.Q4AvgMarginRuns();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewData.Model);
    }

    // -----------------------------------------------------------------------
    // Q4.1 — Highest average margin (wickets)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Q4_1AvgMarginWickets_ParsesWicketsAndFindsHighestAverage()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q4_1AvgMarginWickets();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AverageMarginResult>(viewResult.ViewData.Model);
        // South Africa: 3 wickets avg 3.0, Australia: 5 wickets avg 5.0
        Assert.Equal("Australia", model.Team);
        Assert.Equal(5.0, model.AverageMargin);
    }

    // -----------------------------------------------------------------------
    // Q5 — Matches above average margin (runs)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Q5AboveAvgMargin_ReturnsMatchesAboveTheAverage()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q5AboveAvgMargin();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<T20Match>>(viewResult.ViewData.Model).ToList();
        // Seed runs margins: 61, 135, 40 → average = 78.67
        // Above average: 135 only
        Assert.Single(model);
        Assert.Equal("135 runs", model.First().Margin);
    }

    [Fact]
    public async Task Q5AboveAvgMargin_ReturnsEmptyList_WhenNoDB()
    {
        var controller = new ReportsController(GetEmptyDbContext());

        var result = await controller.Q5AboveAvgMargin();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<T20Match>>(viewResult.ViewData.Model);
        Assert.Empty(model);
    }

    // -----------------------------------------------------------------------
    // Q6 — Most wins while chasing (wickets)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Q6MostWinsChasing_ReturnsRank1Team()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q6MostWinsChasing();

        var viewResult = Assert.IsType<ViewResult>(result);
        // O controller retorna uma lista — o time com rank 1 deve estar no topo
        var model = Assert.IsAssignableFrom<IEnumerable<ChasingWinsResult>>(viewResult.ViewData.Model).ToList();
        Assert.NotEmpty(model);
        Assert.Equal(1, model.First().Rank);
        // Wicket wins: SA×1, Australia×1 — first alphabetically or by insertion order wins
        // Either way the rank must be 1
    }

    [Fact]
    public async Task Q6MostWinsChasing_Excludes_TiedAndNoResult()
    {
        var ctx = GetEmptyDbContext();
        // Only a tied wickets result (edge case: "tied" winner with wicket margin)
        ctx.Matches.Add(new T20Match { Team1 = "A", Team2 = "B", Winner = "tied", Margin = "5 wickets", MatchDate = DateTime.Now, Ground = "X" });
        ctx.SaveChanges();
        var controller = new ReportsController(ctx);

        var result = await controller.Q6MostWinsChasing();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = viewResult.ViewData.Model;
        Assert.True(model == null || !((IEnumerable<ChasingWinsResult>)model).Any());
    }

    // -----------------------------------------------------------------------
    // Q7 — Head to head aggregate
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Q7HeadToHead_ReturnsAggregatePerWinner()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q7HeadToHead("India", "South Africa");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<HeadToHeadAggregate>>(viewResult.ViewData.Model).ToList();

        Assert.Equal(2, model.Count); // India and South Africa each won at least once
        var indiaRow = model.Single(r => r.Winner == "India");
        var saRow    = model.Single(r => r.Winner == "South Africa");
        Assert.Equal(2, indiaRow.MatchesWon);
        Assert.Equal(1, saRow.MatchesWon);
    }

    [Fact]
    public async Task Q7HeadToHead_ReturnsEmpty_WhenTeamsNeverMet()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q7HeadToHead("Pakistan", "New Zealand");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<HeadToHeadAggregate>>(viewResult.ViewData.Model);
        Assert.Empty(model);
    }

    // -----------------------------------------------------------------------
    // Q8 — Month in 2024 with most matches
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Q8MonthMostMatches_ReturnsMonthsOrderedByMatchCount()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q8MonthMostMatches();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<MonthMatchesResult>>(viewResult.ViewData.Model).ToList();

        // June has 4 matches, March has 3 → June should be first
        // Agora ele só confere se COMEÇA com "Jun". 
        // Funciona tanto para "Jun" quanto para "June"!
        Assert.StartsWith("Jun", model.First().MonthName); 
        Assert.Equal(4, model.First().MatchesPlayed);
    }

    [Fact]
    public async Task Q8MonthMostMatches_Excludes_Non2024_Matches()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q8MonthMostMatches();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<MonthMatchesResult>>(viewResult.ViewData.Model).ToList();

        // The 2023 match is in September — must NOT appear
        Assert.DoesNotContain(model, m => m.MonthName == "September");
    }

    // -----------------------------------------------------------------------
    // Q9 — Win percentage in 2024
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Q9WinPercentage_CalculatesCorrectPercentage()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q9WinPercentage();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<WinPercentageResult>>(viewResult.ViewData.Model).ToList();

        // India: played 3, won 2 → 66.67%
        var india = model.Single(t => t.Team == "India");
        Assert.Equal(3, india.MatchesPlayed);
        Assert.Equal(2, india.Wins);
        Assert.Equal(66.67m, india.WinPercentage);
    }

    [Fact]
    public async Task Q9WinPercentage_IsOrderedByPercentageDescending()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q9WinPercentage();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<WinPercentageResult>>(viewResult.ViewData.Model).ToList();
        for (int i = 1; i < model.Count; i++)
            Assert.True(model[i].WinPercentage <= model[i - 1].WinPercentage);
    }

    [Fact]
    public async Task Q9WinPercentage_Excludes_2023_Matches()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q9WinPercentage();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<WinPercentageResult>>(viewResult.ViewData.Model).ToList();

        // India's 2023 win vs Australia must not inflate the 2024 count
        var india = model.SingleOrDefault(t => t.Team == "India");
        if (india != null)
            Assert.Equal(3, india.MatchesPlayed); // only 2024 matches
    }

    // -----------------------------------------------------------------------
    // Q10 — Most successful team at each ground
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Q10GroundSuccess_ReturnsTopTeamPerGround()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q10GroundSuccess();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<GroundSuccessResult>>(viewResult.ViewData.Model).ToList();

        // Melbourne: Australia×1, Australia = winner (tied and no result excluded)
        var melbourne = model.Single(g => g.Ground == "Melbourne");
        Assert.Equal("Australia", melbourne.MostSuccessfulTeam);
        Assert.Equal(1, melbourne.Wins);
    }

    [Fact]
    public async Task Q10GroundSuccess_Excludes_TiedAndNoResult()
    {
        var ctx = GetEmptyDbContext();
        ctx.Matches.AddRange(
            new T20Match { Team1 = "A", Team2 = "B", Winner = "tied",      Margin = "tied",      MatchDate = DateTime.Now, Ground = "Eden" },
            new T20Match { Team1 = "A", Team2 = "B", Winner = "no result", Margin = "no result", MatchDate = DateTime.Now, Ground = "Eden" }
        );
        ctx.SaveChanges();
        var controller = new ReportsController(ctx);

        var result = await controller.Q10GroundSuccess();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<GroundSuccessResult>>(viewResult.ViewData.Model).ToList();
        Assert.Empty(model); // no valid winners at any ground
    }

    [Fact]
    public async Task Q10GroundSuccess_IsOrderedByGroundNameAscending()
    {
        var controller = new ReportsController(GetDbContext());

        var result = await controller.Q10GroundSuccess();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<GroundSuccessResult>>(viewResult.ViewData.Model).ToList();

        var grounds = model.Select(g => g.Ground).ToList();
        Assert.Equal(grounds.OrderBy(g => g).ToList(), grounds);
    }
}
