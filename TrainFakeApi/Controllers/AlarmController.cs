using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TrainFakeApi.Controllers
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

    [HttpGet]
    public IEnumerable<Alarm> Get([FromQuery] int groupId, [FromHeader] string trainId)
    {
      const int groupSize = 5;
      var rng = new Random();
      return Enumerable.Range(1, 5).Select(index => new Alarm
      {
        Date = DateTime.UtcNow,
        AlarmId = Guid.NewGuid(),
        TrainId = trainId,
        ResourceId = index + ((groupId - 1) * groupSize),
        Status = 0
      })
      .ToArray();
    }
  }
}
