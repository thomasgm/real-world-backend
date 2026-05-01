using CricketStatsPro.Mvc.Models;

namespace CricketStatsPro.Mvc.Models;

// Q2, Q3
public class TeamWinsRank
{
    public string Team { get; set; } = string.Empty;
    public int Wins { get; set; }
    public int Rank { get; set; }

    public int Year { get; set; }
}

// Q4, Q4.1
public class AverageMarginResult
{
    public string Team { get; set; } = string.Empty;
    public double AverageMargin { get; set; }
}

// Q6
public class ChasingWinsResult
{
    public string Team { get; set; } = string.Empty;
    public int WinsWhileChasing { get; set; }
    public int Rank { get; set; }
}

// Q7
public class HeadToHeadAggregate
{
    public string TeamA { get; set; } = string.Empty;
    public string TeamB { get; set; } = string.Empty;
    public string Winner { get; set; } = string.Empty;
    public int MatchesWon { get; set; }
}

// Q8
public class MonthMatchesResult
{
    public string MonthName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int MatchesPlayed { get; set; }
}

// Q9
public class WinPercentageResult
{
    public string Team { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public decimal WinPercentage { get; set; }
}

// Q10
public class GroundSuccessResult
{
    public string Ground { get; set; } = string.Empty;
    public string MostSuccessfulTeam { get; set; } = string.Empty;
    public int Wins { get; set; }
}
