using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HorseRacing.Models;

namespace HorseRacing.Controllers
{
    public class RaceController : Controller
    {
        private static List<Race> races = new List<Race>();
        private static List<Bet> bets = new List<Bet>();
        private static Mutex mutex = new Mutex();

        public RaceController()
        {
            if (!races.Any())
            {
                InitializeRaces();
            }
        }

        private void InitializeRaces()
        {
            races.Add(new Race
            {
                RaceId = 1,
                StartTime = DateTime.Now.AddMinutes(10),
                Horses = new List<Horse>
                {
                    new Horse { HorseId = 1, Name = "Lightning", Odds = 2.5 },
                    new Horse { HorseId = 2, Name = "Thunder", Odds = 3.0 },
                    new Horse { HorseId = 3, Name = "Storm", Odds = 4.0 }
                }
            });

            races.Add(new Race
            {
                RaceId = 2,
                StartTime = DateTime.Now.AddMinutes(20),
                Horses = new List<Horse>
                {
                    new Horse { HorseId = 4, Name = "Blaze", Odds = 2.0 },
                    new Horse { HorseId = 5, Name = "Flash", Odds = 3.5 },
                    new Horse { HorseId = 6, Name = "Bolt", Odds = 5.0 }
                }
            });
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
        public IActionResult PlaceBet(int raceId, int horseId, string userName, double amount)
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
                    HorseName = horse.Name, // Przypisywanie nazwy konia
                    UserName = userName,
                    Amount = amount,
                    IsWinningBet = false // To be determined after race
                };
                bets.Add(bet);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            return RedirectToAction("Index");
        }

        public IActionResult SimulateRace(int raceId)
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
