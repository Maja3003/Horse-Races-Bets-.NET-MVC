using HorseRacing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HorseRacing.Hubs
{
    public class RaceHub : Hub
    {
        public async Task SendRaceUpdate(object raceData)
        {
            await Clients.All.SendAsync("ReceiveRaceUpdate", raceData);
        }
    }
}
