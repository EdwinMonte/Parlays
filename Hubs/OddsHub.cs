using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Parlays.Models;

namespace Parlays.Hubs
{
    public class OddsHub : Hub
    {
        public async Task JoinSportGroup(string sportName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sportName);
        }

        public async Task LeaveSportGroup(string sportName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sportName);
        }
    }
}
