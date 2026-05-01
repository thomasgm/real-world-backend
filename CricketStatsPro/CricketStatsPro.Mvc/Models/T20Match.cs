using System.ComponentModel.DataAnnotations;

namespace CricketStatsPro.Mvc.Models;

public class T20Match
{
    [Key]
    public string Team1 { get; set; } = string.Empty;
    public string Team2 { get; set; } = string.Empty;
    public string Winner { get; set; } = string.Empty;
    public string Margin { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; } = DateTime.MinValue;
    public string Ground { get; set; } = string.Empty;
}
