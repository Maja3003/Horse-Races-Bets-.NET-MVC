using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HorseRacing.Models;
using Microsoft.AspNetCore.SignalR;
using HorseRacing.Hubs;
using System.Numerics;

namespace HorseRacing.Controllers
{
    public class RaceController : Controller
    {
        private static List<Race> races = new List<Race>();
        private static List<Bet> bets = new List<Bet>();
        private static Mutex mutex = new Mutex();
        private readonly IHubContext<BettingHub> _hubContext;

        public RaceController(IHubContext<BettingHub> hubContext)
        {
            _hubContext = hubContext;

            if (!races.Any())
            {
                InitializeRaces();
            }
        }

        private void InitializeRaces()
        {
            var horseNames = new List<string>
    {
        "Thunderbolt", "Shadowfax", "Black Beauty", "Silver Streak", "Midnight Star",
        "Golden Glory", "Storm Chaser", "Mystic Wind", "Lightning Strike", "Desert Mirage",
        "Whispering Pines", "Crimson Tide", "Blue Moon", "White Knight", "Firestorm",
        "Emerald Dawn", "Silent Thunder", "Velvet Dream", "Phantom Rider", "Sapphire Sky",
        "Shadow Dancer", "Majestic Spirit", "Noble Heart", "Frostbite", "Silver Blaze", "Eclipse",
        "Hurricane", "Falcon"
    };

            var random = new Random();
            int horseNameIndex = 0;

            var startTimes = new List<int> { 120, 360, 600 }; // 2 minutes, 6 minutes, 10 minutes

            for (int raceId = 1; raceId <= 3; raceId++)
            {
                var horseCount = random.Next(8, 10); // Random number of horses between 8 and 9
                var horses = new List<Horse>();

                for (int i = 0; i < horseCount; i++)
                {
                    var winnerOdds = Math.Round(random.NextDouble() * 5 + 1, 2); // Random odds between 1 and 6
                    var otherOdds = Math.Round(winnerOdds * (random.NextDouble() * 0.5 + 0.5), 2); // Random odds between 50% and 100% of winnerOdds

                    horses.Add(new Horse
                    {
                        HorseId = i + 1,
                        Name = horseNames[horseNameIndex % horseNames.Count],
                        WinnerOdds = winnerOdds,
                        OtherOdds = otherOdds
                    });
                    horseNameIndex++;
                }

                races.Add(new Race
                {
                    RaceId = raceId,
                    Horses = horses,
                    StartTime = DateTime.Now.AddSeconds(startTimes[raceId - 1]) // Set start time to 2, 6, 10 minutes from now
                });
            }
        }



        public IActionResult Index()
        {
            return View(races);
        }

        public IActionResult PlaceBet(int raceId)
        {
            var race = races.FirstOrDefault(r => r.RaceId == raceId);
            return View(race);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceBet(int raceId, int horseId, string userName, double amount)
        {
            mutex.WaitOne();
            try
            {
                var race = races.FirstOrDefault(r => r.RaceId == raceId);
                var horse = race?.Horses.FirstOrDefault(h => h.HorseId == horseId);

                if (horse == null)
                {
                    return BadRequest("Invalid horse ID");
                }

                var bet = new Bet
                {
                    BetId = bets.Count + 1,
                    RaceId = raceId,
                    HorseId = horseId,
                    HorseName = horse.Name,
                    UserName = userName,
                    Amount = amount,
                    IsWinningBet = false // To be determined after race
                };
                bets.Add(bet);

                // Wyœlij aktualizacjê zak³adu do wszystkich klientów
                await _hubContext.Clients.All.SendAsync("ReceiveBet", bet);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> SimulateRace(int raceId)
        {
            var race = races.FirstOrDefault(r => r.RaceId == raceId);
            if (race != null)
            {
                var random = new Random();
                var winningHorse = race.Horses[random.Next(race.Horses.Count)];
                winningHorse.IsWinner = true;

                foreach (var bet in bets.Where(b => b.RaceId == raceId))
                {
                    if (bet.HorseId == winningHorse.HorseId)
                    {
                        bet.IsWinningBet = true;
                    }
                }
                await _hubContext.Clients.All.SendAsync("ReceiveRaceUpdate", raceId, winningHorse.HorseId);
            }
            return RedirectToAction("Index");
        }

        public IActionResult ViewBets()
        {
            return View(bets);
        }

        [HttpGet]
        public JsonResult GetRaceUpdates()
        {
            mutex.WaitOne();
            try
            {
                return Json(races);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }
}
