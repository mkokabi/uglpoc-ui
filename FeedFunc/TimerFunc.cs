using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace UGL.PoC
{
  public static class TimerFunc
  {
    [FunctionName("TimerFunc5")]
    public static async Task Run5([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer, ILogger log)
    {
      log.LogInformation($"C# Timer trigger function 5 executed at: {DateTime.Now}");
      await InsertEventOfCategory("A");
    }

    [FunctionName("TimerFunc10")]
    public static async Task Run10([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer, ILogger log)
    {
      log.LogInformation($"C# Timer trigger function 10 executed at: {DateTime.Now}");
      await InsertEventOfCategory("B");
    }

    [FunctionName("TimerFunc20")]
    public static async Task Run20([TimerTrigger("*/20 * * * * *")] TimerInfo myTimer, ILogger log)
    {
      log.LogInformation($"C# Timer trigger function 20 executed at: {DateTime.Now}");
      await InsertEventOfCategory("C");
    }
    private static async Task InsertEventOfCategory(string category)
    {
      var connStr = "Server=tcp:uglpoc.database.windows.net,1433;Initial Catalog=EventsDb;Persist Security Info=False;User ID=sqladmin;Password=Passw0rd!2345;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
      // var connStr = "Server=localhost;Initial Catalog=EventsDb;Persist Security Info=False;User ID=sqladmin;Password=Passw0rd!2345;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
      var names = new string[5] { "A", "AB", "AC", "AD", "AE" };
      using (SqlConnection conn = new SqlConnection(connStr))
      {
        conn.Open();
        for (int i = 0; i < (category == "A" ? 50 : category == "B" ? 100 : 150); i++)
        {
          Random _random = new Random();
          var name = names[_random.Next(0, 4)];
          string values = GenerateSensorValues(category == "A" ? 50 : category == "B" ? 100 : 200);
          var text = $"Insert into [dbo].[Events] values (NewId(), GETUTCDATE(), '{ name }', '{ category }', '{ values}')";

          using (SqlCommand cmd = new SqlCommand(text, conn))
          {
            // Execute the command and log the # rows affected.
            var rows = await cmd.ExecuteNonQueryAsync();
          }
        }
      }
    }

    private static string GenerateSensorValues(int num)
    {
      var dict = new Dictionary<string, string>();
      for (int i = 0; i < num; i++)
      {
        dict.Add(Guid.NewGuid().ToString(), $"Values: {Guid.NewGuid()}  {Guid.NewGuid()}");
      }
      return JsonSerializer.Serialize(dict);
    }
  }
}
