using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SimuladorApi.Hubs
{
    [Authorize]
    public class RealtimeHub : Hub
    {
    }
}