using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace UGL.PoC
{
  public static class FeedCacheFunc
  {
    private static Lazy<ConnectionMultiplexer> lazyConnection = CreateConnection();

    public static ConnectionMultiplexer Connection
    {
      get => lazyConnection.Value;
    }

    private static Lazy<ConnectionMultiplexer> CreateConnection()
    {
      return new Lazy<ConnectionMultiplexer>(() =>
      {
        var cacheConnection = Environment.GetEnvironmentVariable("RedisConnection");
        return ConnectionMultiplexer.Connect(cacheConnection);
      });
    }

    public static IDatabase GetDatabase() => Connection.GetDatabase();

    public static System.Net.EndPoint[] GetEndPoints() => Connection.GetEndPoints();

    public static IServer GetServer(string host, int port) =>  Connection.GetServer(host, port);

    public class Change
    {
      public int PrevValue { get; set; }
      public int CurrValue { get; set; }
    }

    [FunctionName("FeedCacheFunc")]
    public static async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer, ILogger log)
    {
      IDatabase cache = GetDatabase();

      int counter = await GetPreviousCounter(cache);

      Response res = await CallTrainApi(counter);

      string currStateJson = GetCurrentState(res);

      await StoreStateInRedis(cache, counter, res, currStateJson);

      await BroadcastStatusChange();
    }

    private static async Task BroadcastStatusChange()
    {
      var backendAPIHttpClient = new HttpClient();
      var backendUrl = Environment.GetEnvironmentVariable("BackendApiURL");
      var backendMessageUrl = $"{backendUrl}/Alarm?message=alarmchange";
      await backendAPIHttpClient.PostAsync(backendMessageUrl, null);
    }

    private static async Task StoreStateInRedis(IDatabase cache, int counter, Response res, string currStateJson)
    {
      await cache.StringSetAsync("CurrentState", currStateJson);
      await cache.StringSetAsync(res.src[0].cfg_hash, currStateJson);
      await cache.StringSetAsync("Counter", res.src[0].last_trans_id);
      Console.WriteLine($"cfg_hash: {res.src[0].cfg_hash} starting_counter: {counter}, Last_counter:{res.src[0].last_trans_id}");
    }

    private static string GetCurrentState(Response res)
    {
      var currState = new Dictionary<string, int>();

      res.src[0].alarm_journal.ToList().ForEach(alarm =>
      {
        string key = alarm.alarmid.ToString();
        int status = alarm.status;
        currState[key] = status;
      });

      var currStateJson = JsonSerializer.Serialize(currState);
      return currStateJson;
    }

    private static async Task<Response> CallTrainApi(int counter)
    {
      var trainAPIHttpClient = new HttpClient();
      var trainUrl = Environment.GetEnvironmentVariable("TrainUrl");
      var trainAlarmsUrl = $"{trainUrl}/alarmsjournal?area=VIEW&prev_trans_id={counter}";
      var response = await trainAPIHttpClient.GetStringAsync(trainAlarmsUrl);
      var res = JsonSerializer.Deserialize<Response>(response);
      return res;
    }

    private static async Task<int> GetPreviousCounter(IDatabase cache)
    {
      var counter = 4238;
      if (await cache.KeyExistsAsync("Counter"))
      {
        counter = (int)(await cache.StringGetAsync("Counter"));
      }

      return counter;
    }
  }
}
