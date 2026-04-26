/*
CREATE TABLE IPLPlayers (
    Player	NVARCHAR(120),
    Price_in_cr	DECIMAL(10, 2),
    Type	NVARCHAR(50),
    Acquisition	NVARCHAR(50),
    Role	NVARCHAR(50),
    Team	NVARCHAR(100)
);
Mapeando essa tabela com a classe Player, onde cada propriedade da classe corresponde a uma coluna da tabela.
*/
using System.ComponentModel.DataAnnotations;
namespace IPLAnalysis.Mvc.Models
{
    public class Player
    {
        [Key]
        public int Id { get; set; } // Chave primária para a tabela

        [Required]
        public required string PlayerName { get; set; } // Correspondente à coluna "Player"

        [Required]
        public required decimal Price_in_cr { get; set; } // Correspondente à coluna "Price_in_cr"

        [Required]
        public required string Type { get; set; } // Correspondente à coluna "Type"

        [Required]
        public required string Acquisition { get; set; } // Correspondente à coluna "Acquisition"

        [Required]
        public required string Role { get; set; } // Correspondente à coluna "Role"

        [Required]
        public required string Team { get; set; } // Correspondente à coluna "Team"

    }
}