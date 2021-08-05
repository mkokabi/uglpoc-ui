using System;
namespace UGL.PoC
{
  public class Alarm
  {
    public DateTime Date { get; set; }

    public Guid AlarmId { get; set; }

    public string TrainId { get; set; }

    public int ResourceId { get; set; }

    public int Status { get; set; }
  }

  public class Response 
  {
    public Src[] src { get; set; }
  }

  public class Src
  {
    public Journal[] alarm_journal { get; set; }
    public string cfg_hash { get; set; }
    public int last_trans_id { get; set; }
    public string source { get; set; }
  }

  public class Journal {
    public int alarmid { get; set; }
    public float[][] meta { get; set; }
    public int status { get; set; }
    public int tid { get; set; }
    public float timestamp { get; set; }
  }
}