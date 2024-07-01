using Microsoft.AspNetCore.Mvc;

namespace HorseRacing.Models
{
    public class Race
    {
        public int RaceId { get; set; }
        public List<Horse> Horses { get; set; }
        public bool IsSimulated { get; set; }

        public Race()
        {
            Horses = new List<Horse>();
        }

    }

    public class Horse
    {
        public int HorseId { get; set; }
        public string Name { get; set; }
        public double CurrentSpeed { get; set; }
        public double DistanceCovered { get; set; } = 0;
        public double WinnerOdds { get; set; }
        public double OtherOdds { get; set; }
        public bool IsWinner { get; set; }
    }
}
