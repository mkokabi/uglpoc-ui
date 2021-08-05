using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AlarmApi.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class AlarmController : ControllerBase
  {
    private readonly ILogger<AlarmController> _logger;

    public AlarmController(ILogger<AlarmController> logger)
    {
      _logger = logger;
    }

    private static Lazy<ConnectionMultiplexer> lazyConnection = CreateConnection();

    public static ConnectionMultiplexer Connection
    {
      get => lazyConnection.Value;
    }

    private static Lazy<ConnectionMultiplexer> CreateConnection()
    {
      return new Lazy<ConnectionMultiplexer>(() =>
      {
        string cacheConnection = "127.0.0.1:6379";
        return ConnectionMultiplexer.Connect(cacheConnection);
      });
    }

    public static IDatabase GetDatabase() => Connection.GetDatabase();

    public static System.Net.EndPoint[] GetEndPoints() => Connection.GetEndPoints();

    public static IServer GetServer(string host, int port) => Connection.GetServer(host, port);
    [HttpGet]
    public async Task<Dictionary<string, int>> Get()
    {
      IDatabase cache = GetDatabase();

      var currStateJson = await cache.StringGetAsync("CurrentState");
      var currState = JsonSerializer.Deserialize<Dictionary<string, int>>(currStateJson);

      return currState;
    }
  }
}
