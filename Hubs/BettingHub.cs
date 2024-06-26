using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace HorseRacing.Hubs
{
    public class BettingHub : Hub
    {
        public async Task PlaceBet(string userName, int raceId, int horseId, double amount)
        {
            // Broadcast the bet to all connected clients
            await Clients.All.SendAsync("ReceiveBet", userName, raceId, horseId, amount);
        }

        public async Task UpdateRace(int raceId)
        {
            // Broadcast race update to all connected clients
            await Clients.All.SendAsync("ReceiveRaceUpdate", raceId);
        }
    }
}
