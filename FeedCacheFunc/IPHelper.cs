using System;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace UGL.PoC
{
  public static class IPHelper
  {
    [FunctionName("IPHelper")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
      HttpClient client = new HttpClient();
      var response = await client.GetAsync("https://api.ipify.org?format=json");
      var content = await response.Content.ReadAsStringAsync();

      return new OkObjectResult(content);
    }
  }
}
