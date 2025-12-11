using Microsoft.AspNetCore.SignalR;

namespace AviFinal.Api.Hubs
{
    public class LocationHub : Hub
    {
        // Called by MapView to request all clients to send location
        public async Task RequestLiveLocations()
        {
            await Clients.All.SendAsync("SendYourLocation");
        }

        // Client will send location here
        public async Task UpdateLocation(LocationDto loc)
        {
            // Send this location to all map viewers
            await Clients.All.SendAsync("ReceiveLocation", loc);
        }
    }
    public class LocationDto
    {
        public string UserName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; }
        public DateTime DeviceTimestamp { get; set; }
    }


}
