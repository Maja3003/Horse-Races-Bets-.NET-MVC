namespace HorseRacing.Models
{
    public class Bet
    {
        public int BetId { get; set; }
        public int RaceId { get; set; }
        public string UserName { get; set; }
        public int HorseId { get; set; }
        public double Amount { get; set; }
        public bool IsWinningBet { get; set; }
    }
}
