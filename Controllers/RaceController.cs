using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HorseRacing.Models;
using Microsoft.AspNetCore.SignalR;
using HorseRacing.Hubs;
using HorseRacing.Data;

namespace HorseRacing.Controllers
{
    public class RaceController : Controller
    {
        private static List<Race> races = new List<Race>();
        private static Mutex mutex = new Mutex();
        private readonly IHubContext<BettingHub> _hubContext;
        private readonly ApplicationDbContext _context;

        public RaceController(IHubContext<BettingHub> hubContext, ApplicationDbContext context)
        {
            _hubContext = hubContext;
            _context = context;

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
        public async Task<IActionResult> PlaceBet(int raceId, List<Bet> bets, string userName, double amount)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Login");
            }

            mutex.WaitOne();
            try
            {
                var race = races.FirstOrDefault(r => r.RaceId == raceId);

                if (race == null)
                {
                    return BadRequest("Invalid race ID");
                }

                foreach (var bet in bets)
                {
                    var horse = race.Horses.FirstOrDefault(h => h.Name == bet.HorseName);

                    if (horse == null)
                    {
                        return BadRequest("Invalid horse name");
                    }

                    var newBet = new Bet
                    {
                        RaceId = raceId,
                        HorseId = horse.HorseId,
                        HorseName = horse.Name,
                        UserName = userName,
                        Amount = amount,
                        IsWinningBet = false
                    };
                    _context.Bets.Add(newBet);
                    await _context.SaveChangesAsync();

                    // Send the new bet update to all clients
                    await _hubContext.Clients.All.SendAsync("ReceiveBet", newBet);
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            return Ok();
        }

        public async Task<IActionResult> SimulateRace(int raceId)
        {
            var race = races.FirstOrDefault(r => r.RaceId == raceId);
            if (race != null)
            {
                var random = new Random();
                var winningHorse = race.Horses[random.Next(race.Horses.Count)];
                winningHorse.IsWinner = true;

                foreach (var bet in _context.Bets.Where(b => b.RaceId == raceId))
                {
                    if (bet.HorseId == winningHorse.HorseId)
                    {
                        bet.IsWinningBet = true;
                    }
                }
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("ReceiveRaceUpdate", raceId, winningHorse.HorseId);
            }
            return RedirectToAction("Index");
        }

        public IActionResult ViewBets()
        {
            var allBets = _context.Bets.OrderByDescending(b => b.BetId).Take(25).ToList();
            return View(allBets);
        }

        public IActionResult MyBets()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Login");
            }

            var userName = User.Identity.Name;
            var userBets = _context.Bets.Where(b => b.UserName == userName).OrderByDescending(b => b.BetId).Take(25).ToList();
            return View(userBets);
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
