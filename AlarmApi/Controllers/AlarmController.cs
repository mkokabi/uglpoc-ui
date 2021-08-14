using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using AlarmApi.Hubs;

namespace AlarmApi.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class AlarmController : ControllerBase
  {
    private readonly ILogger<AlarmController> _logger;
    private readonly IHubContext<AlarmHub, IMessageClient> _alarmHub;
    private readonly IConfiguration _configuration;


    public AlarmController(ILogger<AlarmController> logger,
      IHubContext<AlarmHub, IMessageClient> alarmHub,
      IConfiguration configuration)
    {
      _logger = logger;
      _alarmHub = alarmHub;
      _configuration = configuration;
      lazyConnection = CreateConnection();
    }

    private Lazy<ConnectionMultiplexer> lazyConnection;

    private ConnectionMultiplexer Connection
    {
      get => lazyConnection.Value;
    }

    private Lazy<ConnectionMultiplexer> CreateConnection()
    {
      return new Lazy<ConnectionMultiplexer>(() =>
      {
        string cacheConnection = _configuration["RedisConnection"];
        return ConnectionMultiplexer.Connect(cacheConnection);
      });
    }

    private IDatabase GetDatabase() => Connection.GetDatabase();

    private System.Net.EndPoint[] GetEndPoints() => Connection.GetEndPoints();

    private IServer GetServer(string host, int port) => Connection.GetServer(host, port);
    
    public class Alarm{
      public string Id { get; set; }
      public int Status { get; set; }
    }
    [HttpGet]
    public async Task<List<Alarm>> Get(string trainName)
    {
      IDatabase cache = GetDatabase();

      var currStateJson = await cache.StringGetAsync($"CurrentState_{trainName}");
      var currStateDic = JsonSerializer.Deserialize<Dictionary<string, int>>(currStateJson);
      var currState = currStateDic.Keys.ToList().Select(k => 
        new Alarm {Id = k, Status = currStateDic[k]}
        ).ToList();

      return currState;
    }

    [HttpPost]
    public async Task SendMessage([FromBody]Message message)
    {
      Console.WriteLine($"Sending message: {message.Body} at {message.Time}");

      await _alarmHub.Clients.All.ReceiveMessage( message );
    }
  }
}
