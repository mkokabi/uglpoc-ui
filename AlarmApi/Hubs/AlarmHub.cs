using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace AlarmApi.Hubs
{
  public class Message
  {
    public DateTime Time { get; set; }
    public string Body { get; set; }
  }

  public interface IMessageClient
  {
    Task ReceiveMessage(Message message);
  }

  public class AlarmHub : Hub<IMessageClient>
  {
    public async Task SendMessage(Message message)
    {
      await Clients.All.ReceiveMessage(message);
    }
  }
}