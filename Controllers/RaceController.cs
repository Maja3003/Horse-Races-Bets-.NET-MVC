using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using HorseRacing.Hubs;
using System.Threading;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HorseRacing.Models;
using Microsoft.AspNetCore.SignalR;
using HorseRacing.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace HorseRacing.Controllers
{
    public class RaceController : Controller
    {
        private static List<Race> races = new List<Race>();
        private static Mutex mutex = new Mutex();
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<RaceHub> _hubContext;

        public RaceController(ApplicationDbContext context, IHubContext<RaceHub> hubContext)
        {
            _context = context;
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

            for (int raceId = 1; raceId <= 3; raceId++)
            {
                var horseCount = random.Next(8, 10); // Random number of horses between 8 and 9
                var horses = new List<Horse>();
                Random rnd = new Random();

                for (int i = 0; i < horseCount; i++)
                {
                    horses.Add(new Horse
                    {
                        HorseId = i + 1,
                        Name = horseNames[horseNameIndex % horseNames.Count],
                        CurrentSpeed = rnd.Next(20,30),
                        WinnerOdds = 2.0,
                        OtherOdds = 2.0
                    });
                    horseNameIndex++;
                }

                races.Add(new Race
                {
                    RaceId = raceId,
                    Horses = horses,
                    IsSimulated = false
                });
            }

         
        }

        private void StartBackgroundTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(5000); // Check every 20 seconds
                    SimulateDueRaces();
                }
            });
        }

        private void SimulateDueRaces()
        {
            mutex.WaitOne();
            try
            {
                foreach (var race in races)
                {
                    if (!race.IsSimulated)
                    {
                        _ = SimulateRace(race.RaceId); // Changed to use asynchronous call
                    }
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        [HttpPost]
        public async Task<IActionResult> SimulateRace(int raceId)
        {
            StartBackgroundTask();

            var race = races.FirstOrDefault(r => r.RaceId == raceId);
            if (race != null && !race.IsSimulated)
            {

                race.IsSimulated = true;
                var totalDuration = 60; // Total duration in seconds for the race simulation
                var updateInterval = totalDuration / 3; // Update three times during the race

                    UpdateRaceOdds(race);
                    await _hubContext.Clients.All.SendAsync("UpdateRace", race.RaceId, race);

                var random = new Random();

                for (int interval = 1; interval <= 2; interval++)
                {
                    await Task.Delay(updateInterval * 1000); // Delay for each third of the race

                    foreach (var horse in race.Horses)
                    {
                        double speedChange = random.NextDouble() * 4 - 6;
                        horse.CurrentSpeed += speedChange;
                        horse.DistanceCovered += horse.CurrentSpeed > 0 ? horse.CurrentSpeed * updateInterval : 0;
                    }

                    UpdateRaceOdds(race);

                    if (interval == 2)
                    {
                        MarkWinningBets(race); // Update bet results based on the final positions
                        await _hubContext.Clients.All.SendAsync("RaceFinished", race.RaceId, race.Horses.OrderByDescending(h => h.DistanceCovered).Take(3).Select(h => h.Name));
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("UpdateRace", race.RaceId, race);
                    }
                }
                
            }
            return Ok();
        }

        private void UpdateRaceOdds(Race race)
        {
            var sortedHorses = race.Horses.OrderByDescending(h => h.DistanceCovered).ToList();
            double baseOdds = 2.0; // Starting odds for the leader
            int rank = 1;

            // The base odds are lower for the leading horse, and increase for horses behind.
            foreach (var horse in sortedHorses)
            {
                // The factor adjusts the odds inversely to the rank, higher rank (leading horses) get lower odds
                double adjustmentFactor = 1 - 0.1 * (rank - 1);
                horse.WinnerOdds = Math.Round(baseOdds / adjustmentFactor, 2);
                horse.OtherOdds = Math.Round(horse.WinnerOdds * 0.8, 2);
                rank++;
            }

            if (race.IsSimulated) // Assuming race is marked as simulated when finished
            {
                _hubContext.Clients.All.SendAsync("RaceFinished", race.RaceId, sortedHorses.Take(3).Select(h => h.Name).ToList());
            }
        }

        private void MarkWinningBets(Race race)
        {
            var topThreeHorses = race.Horses.OrderByDescending(h => h.DistanceCovered).Take(3).Select(h => h.HorseId).ToList();
            var betsToUpdate = _context.Bets.Where(b => b.RaceId == race.RaceId);

            foreach (var bet in betsToUpdate)
            {
                bet.IsWinningBet = topThreeHorses.Contains(bet.HorseId);
            }

            _context.SaveChanges();
        }

        public IActionResult Index()
        {
            return View(races);
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
                    return BadRequest("Betting for this race is closed.");
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
                        IsWinningBet = false,
                        BetType = bet.BetType
                    };
                    _context.Bets.Add(newBet);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine(ex);
                return StatusCode(500, "Internal server error");
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            return Ok();
        }

        public IActionResult ViewBets()
        {
            var allBets = _context.Bets
                .OrderByDescending(b => b.BetId)
                .Take(15)
                .ToList();
            return View(allBets);
        }
        public IActionResult MyBets()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Login");
            }
            var userName = User.Identity.Name;
            var userBets = _context.Bets
                .Where(b => b.UserName == userName)
                .OrderByDescending(b => b.BetId)
                .Take(15)
                .ToList();
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
