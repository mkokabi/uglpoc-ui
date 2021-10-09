using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentCsv.FluentReader;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Blob;

namespace TM.NDVR
{
  public static class TimesheetInQueue
  {
    private static HttpClient httpClient = new HttpClient();

    [FunctionName("TimesheetInQueue")]
    public static async Task Run(
      [ServiceBusTrigger("timesheets", Connection = "ndvrcltimesheet_SERVICEBUS")] string myQueueItem,
      [Blob("clexport", FileAccess.Read, Connection = "ndvrcltimesheet_STORAGE")] CloudBlobContainer blobContainer,
      ILogger log)
    {
      log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
      var contentCsv = ReadContentFromBlob(myQueueItem, blobContainer, log);
      var psApiUrl = GetEnvironmentVariable("PSApiURL");
      // var response = await httpClient.PostAsJsonAsync(psApiUrl, contentCsv);
      var json = JsonSerializer.Serialize(contentCsv);
      log.LogInformation(json);
      var response = await httpClient.PostAsync(psApiUrl, new StringContent(
        json, Encoding.UTF8, "application/json"
      ));
      log.LogInformation($"Response status {response.StatusCode}");
      if (!response.IsSuccessStatusCode)
      {
        log.LogError(await response.Content.ReadAsStringAsync());
      }
    }

    private static string GetEnvironmentVariable(string name)
    {
      return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }

    private static PunchTime[] ReadContentFromBlob(string fileName, CloudBlobContainer blobContainer, ILogger log)
    {
      var myBlob = blobContainer.GetBlockBlobReference(fileName);

      log.LogInformation($"Blob info: {myBlob.Uri}");

      var content = myBlob.DownloadText();
      log.LogDebug($"Content: {content}");
      return StringToPunchTimes(fileName, content, log);
    }

    public static PunchTime[] StringToPunchTimes(string fileName, string content, ILogger log)
    {
      var csv = Read.Csv.FromString(content)
              .With.ColumnsDelimiter(",").And.EndOfLineDelimiter("\n")
              .ThatReturns.ArrayOf<PunchTime>()
              .Put.Column(0).As<int>().Into(a => a.SessionNumber)
              .Put.Column(1).Into(a => a.EmployeeID)
              .Put.Column(2).As<int>().Into(a => a.EmployeeRecord)
              .Put.Column(3).Into(a => a.PunchType)
              .GetAll();
      if (csv.Errors.Any())
      {
        log.LogError("Error in parsing {fileName}, {errors}", fileName, csv.Errors);
      }
      return csv.ResultSet;
    }

    public class PunchTime
    {
      [JsonPropertyName("ST_INSTANCE")]
      public int SessionNumber { get; set; }

      [JsonPropertyName("EMPLID")]
      public string EmployeeID { get; set; }

      [JsonPropertyName("EMPL_RCD")]
      public int EmployeeRecord { get; set; }

      [JsonPropertyName("PUNCH_TYPE")]
      public string PunchType { get; set; }
    }
  }
}
