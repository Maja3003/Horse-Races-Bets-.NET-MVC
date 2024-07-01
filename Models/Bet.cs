using System.ComponentModel.DataAnnotations;

namespace HorseRacing.Models
{
    public class Bet
    {
        [Key]
        public int BetId { get; set; }
        public int RaceId { get; set; }
        public string UserName { get; set; }
        public int HorseId { get; set; }
        public string HorseName { get; set; }
        public double Amount { get; set; }
        public string BetType { get; set; }
        public bool IsWinningBet { get; set; }
    }
}
