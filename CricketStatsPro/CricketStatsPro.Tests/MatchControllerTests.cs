using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CricketStatsPro.Mvc.Controllers;
using CricketStatsPro.Mvc.Data;
using CricketStatsPro.Mvc.Models;
using Xunit;

namespace CricketStatsPro.Tests;

public class MatchesControllerTests
{
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
            new() { Team1 = "India",        Team2 = "South Africa", Winner = "India",        Margin = "61 runs",   MatchDate = new DateTime(2024, 3, 27), Ground = "Durban" },
            new() { Team1 = "South Africa", Team2 = "India",        Winner = "South Africa", Margin = "3 wickets", MatchDate = new DateTime(2024, 3, 29), Ground = "Gqeberha" },
            new() { Team1 = "India",        Team2 = "South Africa", Winner = "India",        Margin = "135 runs",  MatchDate = new DateTime(2024, 3, 30), Ground = "Johannesburg" },

            // Australia vs England — 2024 (tied / no result edge cases)
            new() { Team1 = "Australia",    Team2 = "England",      Winner = "tied",         Margin = "tied",      MatchDate = new DateTime(2024, 6,  5), Ground = "Melbourne" },
            new() { Team1 = "England",      Team2 = "Australia",    Winner = "no result",    Margin = "no result", MatchDate = new DateTime(2024, 6,  7), Ground = "Melbourne" },
            new() { Team1 = "Australia",    Team2 = "England",      Winner = "Australia",    Margin = "5 wickets", MatchDate = new DateTime(2024, 6, 10), Ground = "Melbourne" },
            new() { Team1 = "Australia",    Team2 = "England",      Winner = "Australia",    Margin = "40 runs",   MatchDate = new DateTime(2024, 6, 12), Ground = "Sydney" },

            // Partida em 2023 (deve ser excluída por queries com filtro de ano)
            new() { Team1 = "India",        Team2 = "Australia",    Winner = "India",        Margin = "20 runs",   MatchDate = new DateTime(2023, 9,  1), Ground = "Kolkata" },
        });
        context.SaveChanges();
    }


    [Fact]
    public async Task HeadToHead_ReturnsCorrectStats()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new MatchesController(context);

        // Act
        var result = await controller.HeadToHead("India", "South Africa");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HeadToHeadViewModel>(viewResult.ViewData.Model);
        
        Assert.Equal(2, model.Team1Wins); // India
        Assert.Equal(1, model.Team2Wins); // SA
        Assert.Equal(3, model.TotalMatches);
    }

    [Fact]
    public async Task ChasingStats_IdentifiesWicketWins()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new MatchesController(context);

        // Act
        var result = await controller.ChasingStats();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<ChasingViewModel>>(viewResult.ViewData.Model).ToList();
        
        // Seed has 2 wicket wins: South Africa (3 wickets) and Australia (5 wickets)
        Assert.Equal(2, model.Count);
        Assert.Contains(model, m => m.TeamName == "South Africa");
        Assert.Contains(model, m => m.TeamName == "Australia");
    }

    // --- Index ---

    [Fact]
    public async Task Index_WithNoFilter_ReturnsAllMatches()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new MatchesController(context);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<T20Match>>(viewResult.ViewData.Model);
        Assert.Equal(8, model.Count()); // seed has 8 matches
    }

    [Fact]
    public async Task Index_WithGroundFilter_ReturnsOnlyMatchesAtThatGround()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new MatchesController(context);

        // Act
        var result = await controller.Index(ground: "Durban");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<T20Match>>(viewResult.ViewData.Model);
        Assert.Single(model);
        Assert.All(model, m => Assert.Equal("Durban", m.Ground));
    }

    [Fact]
    public async Task Index_WithUnknownGround_ReturnsEmptyList()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new MatchesController(context);

        // Act
        var result = await controller.Index(ground: "Lord's");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<T20Match>>(viewResult.ViewData.Model);
        Assert.Empty(model);
    }

    // --- GroundStats ---

    [Fact]
    public async Task GroundStats_GroupsMatchesByGround()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new MatchesController(context);

        // Act
        var result = await controller.GroundStats();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<GroundViewModel>>(viewResult.ViewData.Model);

        // Seed has 6 unique grounds: Durban, Gqeberha, Johannesburg, Melbourne, Sydney, Kolkata
        Assert.Equal(6, model.Count());
    }

    [Fact]
    public async Task GroundStats_IsOrderedByMatchCountDescending()
    {
        // Arrange — fresh DB with 2 matches at Durban so it should rank first
        var databaseName = $"T20TestDb_{Guid.NewGuid():N}";
        var connectionString = $"Server=localhost;Database={databaseName};User Id=sa;Password=Brasil#102030;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<T20DbContext>()
            .UseSqlServer(connectionString)
            .Options;
        var context = new T20DbContext(options);
        context.Database.EnsureCreated();
        context.Matches.AddRange(
            new T20Match { Team1 = "India",        Team2 = "South Africa", Winner = "India",        Margin = "50 runs",   Ground = "Durban" },
            new T20Match { Team1 = "South Africa", Team2 = "India",        Winner = "South Africa", Margin = "3 wickets", Ground = "Gqeberha" },
            new T20Match { Team1 = "India",        Team2 = "South Africa", Winner = "India",        Margin = "135 runs",  Ground = "Durban" }
        );
        context.SaveChanges();
        var controller = new MatchesController(context);

        // Act
        var result = await controller.GroundStats();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<GroundViewModel>>(viewResult.ViewData.Model).ToList();
        Assert.Equal("Durban", model.First().GroundName);
        Assert.Equal(2, model.First().MatchCount);
    }

    // --- HeadToHead edge cases ---

    [Fact]
    public async Task HeadToHead_WithNoHistoricalMatches_ReturnsZeroCounts()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new MatchesController(context);

        // Act — Pakistan/New Zealand never appear in the seed data
        var result = await controller.HeadToHead("Pakistan", "New Zealand");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HeadToHeadViewModel>(viewResult.ViewData.Model);
        Assert.Equal(0, model.Team1Wins);
        Assert.Equal(0, model.Team2Wins);
        Assert.Equal(0, model.TotalMatches);
    }

    [Fact]
    public async Task HeadToHead_CountsWinsRegardlessOfTeamOrder()
    {
        // Arrange — same seed data but teams passed in reverse order
        var context = GetDbContext();
        var controller = new MatchesController(context);

        // Act
        var result = await controller.HeadToHead("South Africa", "India");

        // Assert — wins must be swapped compared to the default call
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HeadToHeadViewModel>(viewResult.ViewData.Model);
        Assert.Equal(1, model.Team1Wins); // South Africa
        Assert.Equal(2, model.Team2Wins); // India
        Assert.Equal(3, model.TotalMatches);
    }
}
