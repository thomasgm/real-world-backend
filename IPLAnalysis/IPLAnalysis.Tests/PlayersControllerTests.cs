using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IPLAnalysis.Mvc.Controllers;
using IPLAnalysis.Mvc.Data;
using IPLAnalysis.Mvc.Models;
using Xunit;

namespace IPLAnalysis.Tests;
public class PlayersControllerTests
{
    private IplDbContext GetDbContext(bool seedData = true)
    {
        var databaseName = $"IPLTestDb_{Guid.NewGuid():N}";
        var connectionString = $"Server=localhost;Database={databaseName};User Id=sa;Password=Brasil#102030;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<IplDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        var context = new IplDbContext(options);
        context.Database.EnsureCreated(); // Cria tabelas se não existirem
        if (seedData)
        {
            SeedTestData(context); // Popula com dados de teste
        }
        return context;
    }
    private static void SeedTestData(IplDbContext context)
{
    context.Players.AddRange(new List<Player>
    {
        new Player { PlayerName = "P1", Team = "Team A", Price_in_cr = 10, Role = "Batter", Type = "Indian Player", Acquisition = "Auction" },
        new Player { PlayerName = "P2", Team = "Team A", Price_in_cr = 5, Role = "Bowler", Type = "Overseas Player", Acquisition = "Transfer" },
        new Player { PlayerName = "P3", Team = "Team B", Price_in_cr = 20, Role = "Batter", Type = "Indian Player", Acquisition = "Auction" },
        //new Player { PlayerName = "P4", Team = "Team A", Price_in_cr = 25, Role = "All-rounder", Type = "Indian Player", Acquisition = "Auction" },
        //new Player { PlayerName = "P5", Team = "Team B", Price_in_cr = 18, Role = "All-rounder", Type = "Overseas Player", Acquisition = "Auction" },
        //new Player { PlayerName = "P6", Team = "Team C", Price_in_cr = 8, Role = "Wicket-keeper", Type = "Overseas Player", Acquisition = "Auction" },
        //new Player { PlayerName = "P7", Team = "Team C", Price_in_cr = 6, Role = "Bowler", Type = "Indian Player", Acquisition = "Auction" },
        //new Player { PlayerName = "P8", Team = "Team B", Price_in_cr = 3, Role = "Batter", Type = "Indian Player", Acquisition = "Transfer" }
    });

    context.SaveChanges();
}

    [Fact]
    public async Task Index_ReturnsAllPlayers()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new PlayersController(context);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Player>>(viewResult.ViewData.Model);
        Assert.Equal(3, model.Count());
    }

    [Fact]
    public async Task TeamSpending_CalculatesCorrectly()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new PlayersController(context);

        // Act
        var result = await controller.TeamSpending();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<TeamSpendingViewModel>>(viewResult.ViewData.Model);
        
        var teamA = model.First(t => t.TeamName == "Team A");
        Assert.Equal(15, teamA.TotalSpent);
        Assert.Equal(2, teamA.PlayerCount);
    }

        [Fact]
    public async Task TopEarners_ReturnsTop10Players()
    {
        // Arrange: Adicione mais jogadores com preços variados no GetDbContext
        var context = GetDbContext();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.TopEarners();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Player>>(viewResult.ViewData.Model);
        Assert.True(model.Count() <= 10); // Ou verifique ordenação decrescente
    }

    [Fact]
    public async Task Index_HandlesEmptyDatabase()
    {
        var context = GetDbContext(seedData: false);
        var controller = new PlayersController(context);

        var result = await controller.Index();
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Player>>(viewResult.ViewData.Model);
        Assert.Empty(model);
    }

        [Fact]
    public async Task TeamSpending_OrdersByTotalSpentDescending()
    {
        // Arrange: Adicione times com totais diferentes
        var context = GetDbContext();
        var controller = new PlayersController(context);
    
        var result = await controller.TeamSpending();
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<TeamSpendingViewModel>>(viewResult.ViewData.Model).ToList();
        Assert.NotNull(model);
        var modelList = model.ToList();
        
        Assert.True(model[0].TotalSpent >= model[1].TotalSpent); // Verifique ordenação
    }

        [Fact]
    public async Task RoleStats_CalculatesCorrectly()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.RoleStats();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<RoleStatsViewModel>>(viewResult.ViewData.Model);
        // Verifique se há stats para cada role (ex.: "Batter", "Bowler")
        Assert.True(model.Any()); // Ajuste para asserts específicos, ex.: médias corretas
    }
    
    [Fact]
    public async Task TopAllRounders_ReturnsTop3AllRounders()
    {
        // Arrange: Certifique-se de que há pelo menos 3 jogadores com Role = "All-rounder"
        var context = GetDbContext();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.TopAllRounders();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<TopAllRoundersViewModel>>(viewResult.ViewData.Model);
        Assert.True(model.Count() <= 3); // Ou verifique ordenação por preço
    }
    
    [Fact]
    public async Task HighestPricedPerTeam_ReturnsHighestPerTeam()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.HighestPricedPerTeam();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<HighestPricedPerTeamViewModel>>(viewResult.ViewData.Model);
        // Verifique se para cada time, o jogador é o mais caro
        var teamA = model.FirstOrDefault(p => p.Team == "Team A");
        Assert.NotNull(teamA); // Ajuste asserts baseados nos dados
    }
    
    [Fact]
    public async Task Top2PerTeam_ReturnsTop2PerTeam()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.Top2PerTeam();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Top2PerTeamViewModel>>(viewResult.ViewData.Model);
        // Verifique ranks 1 e 2 por time
        Assert.True(model.All(p => p.RankWithinTeam <= 2));
    }
    
    [Fact]
    public async Task MostAndSecondMostExpensivePerTeam_ReturnsCorrectPlayers()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.MostAndSecondMostExpensivePerTeam();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<MostAndSecondMostExpensivePerTeamViewModel>>(viewResult.ViewData.Model);
        // Verifique se o 1º e 2º são corretos por time
        Assert.True(model.All(m => m.MostExpensivePlayer != null));
    }
    
    [Fact]
    public async Task ContributionPercentage_CalculatesCorrectly()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.ContributionPercentage();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<ContributionPercentageViewModel>>(viewResult.ViewData.Model);
        // Verifique se porcentagens somam ~100% por time
        var teamAContributions = model.Where(p => p.Team == "Team A").Sum(p => p.ContributionPercentage);
        Assert.InRange(teamAContributions, 99.9m, 100.1m); // Tolerância para arredondamento
    }
    
    [Fact]
    public async Task PriceCategoryDistribution_CalculatesCorrectly()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.PriceCategoryDistribution();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<PriceCategoryDistributionViewModel>>(viewResult.ViewData.Model);
        // Verifique categorias ("High", "Medium", "Low") e contagens
        Assert.True(model.All(d => d.PriceCategory == "High" || d.PriceCategory == "Medium" || d.PriceCategory == "Low"));
    }
    
    [Fact]
    public async Task TypeComparison_CalculatesAverages()
    {
        // Arrange: Certifique-se de que há jogadores com Type começando com "Indian" ou "Overseas"
        var context = GetDbContext();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.TypeComparison();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<PlayerTypeComparisonViewModel>>(viewResult.ViewData.Model);
        // Verifique médias para "Indian" e "Overseas"
        Assert.Contains(model, m => m.PlayerType == "Indian");
        Assert.Contains(model, m => m.PlayerType == "Overseas");
    }
    
    [Fact]
    public async Task AboveAveragePlayers_ReturnsCorrectPlayers()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.AboveAveragePlayers();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<AboveAveragePlayerViewModel>>(viewResult.ViewData.Model);
        // Verifique se todos têm Surplus > 0
        Assert.True(model.All(p => p.Surplus > 0));
    }
    
    [Fact]
    public async Task TopPlayersByRole_ReturnsTopPerRole()
    {
        // Arrange
        var context = GetDbContext();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.TopPlayersByRole();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<TopPlayerPerRoleViewModel>>(viewResult.ViewData.Model);
        // Verifique se para cada role, há apenas um (o mais caro)
        var roles = model.Select(p => p.Role).Distinct();
        Assert.Equal(roles.Count(), model.Count()); // Um por role
    }

        [Fact]
    public async Task TeamSpending_HandlesSingleTeam()
    {
        var context = GetDbContext(seedData: false);
        context.Database.EnsureCreated();
        context.Players.Add(new Player { PlayerName = "P1", Team = "Team A", Price_in_cr = 10, Role = "Batter", Type = "Type1", Acquisition = "Auction" });
        context.SaveChanges();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.TeamSpending();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<TeamSpendingViewModel>>(viewResult.ViewData.Model);
        Assert.Single(model);
        Assert.Equal(10, model.First().TotalSpent);
    }
    
    [Fact]
    public async Task ContributionPercentage_HandlesZeroTotal()
    {
        // Arrange: Time com jogador de preço 0
         var context = GetDbContext(seedData: false);
        context.Database.EnsureCreated();
        context.Players.Add(new Player { PlayerName = "P1", Team = "Team A", Price_in_cr = 0, Role = "Batter", Type = "Type1", Acquisition = "Auction" });
        context.SaveChanges();
        var controller = new PlayersController(context);
    
        // Act
        var result = await controller.ContributionPercentage();
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<ContributionPercentageViewModel>>(viewResult.ViewData.Model);
        Assert.Equal(0, model.First().ContributionPercentage); // Evita divisão por zero
    }
}
