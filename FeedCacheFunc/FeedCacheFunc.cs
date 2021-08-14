using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
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

    public static IServer GetServer(string host, int port) => Connection.GetServer(host, port);

    public class Change
    {
      public int PrevValue { get; set; }
      public int CurrValue { get; set; }
    }

    private static async Task<Dictionary<string, Response>> CallAllTrainsApi(IDatabase cache, int counter)
    {
      Response res = await CallTrainApi(counter);
      await cache.StringSetAsync("Counter", res.src[0].last_trans_id);

      var trainsPredicates = new Dictionary<string, Func<Journal, bool>> {
        { "A", (Journal journal) => journal.alarmid < 14000 },
        { "B", (Journal journal) => journal.alarmid >= 14000 && journal.alarmid < 18000 },
        { "C", (Journal journal) => journal.alarmid >= 18000 && journal.alarmid < 20000 },
        { "D", (Journal journal) => journal.alarmid >= 23000},
      };

      return trainsPredicates.Keys.ToDictionary(key => key, key =>
      new Response
      {
        src = new[] { new Src {
          alarm_journal = res.src[0].alarm_journal.Where(trainsPredicates[key]).ToArray(),
          cfg_hash = res.src[0].cfg_hash,
          last_trans_id = res.src[0].last_trans_id,
          source = res.src[0].source
        }}
      });
    }

    [FunctionName("FeedCacheFunc")]
    public static async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer, ILogger log)
    {
      IDatabase cache = GetDatabase();

      int counter = await GetPreviousCounter(cache);

      var allTrainsState = await CallAllTrainsApi(cache, counter);

      var trainsNameWithStateChange = new List<string>();
      foreach (var trainState in allTrainsState)
      {
        string prevState = await GetPreviousSateFromRedis(cache, trainState.Key);
        string currStateJson = SerializeState(trainState.Value);
        if (prevState != currStateJson)
        {
          await StoreStateInRedis(cache, trainState.Key, trainState.Value, currStateJson);
          trainsNameWithStateChange.Add(trainState.Key);
        }
      }
      await BroadcastStatusChange(string.Join(",", trainsNameWithStateChange));
    }

    private async static Task<string> GetPreviousSateFromRedis(IDatabase cache, string trainName)
    {
      var state = await cache.StringGetAsync($"CurrentState_{trainName}");
      return state;
    }

    public class Message
    {
      public DateTime Time { get; set; }
      public string Body { get; set; }
    }

    private static async Task BroadcastStatusChange(string message)
    {
      var backendAPIHttpClient = new HttpClient();
      var backendUrl = Environment.GetEnvironmentVariable("BackendApiURL");
      var backendMessageUrl = $"{backendUrl}/Alarm";
      var msg = new Message
      {
        Time = DateTime.UtcNow,
        Body = $"alarmchange_{message}"
      };
      Console.WriteLine($"Broadcasting {msg.Body}");
      var postResp =
      await backendAPIHttpClient.PostAsync(backendMessageUrl,
        new StringContent(JsonSerializer.Serialize(msg), Encoding.UTF8, "application/json"));
      if (!postResp.IsSuccessStatusCode)
      {
        var content = await postResp.Content.ReadAsStringAsync();
        throw new Exception($"Status:{postResp.StatusCode} Content: {content}");
      }
    }

    private static async Task StoreStateInRedis(IDatabase cache, string trainName, Response res, string currStateJson)
    {
      await cache.StringSetAsync($"CurrentState_{trainName}", currStateJson);
      await cache.StringSetAsync(res.src[0].cfg_hash, currStateJson);
      Console.WriteLine($"cfg_hash: {res.src[0].cfg_hash}, Last_counter:{res.src[0].last_trans_id}");
    }

    private static string SerializeState(Response res)
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
