using System;

namespace TrainFakeApi
{
    public class Alarm
    {
        public DateTime Date { get; set; }

        public Guid AlarmId { get; set; }

        public string TrainId {get; set; }

        public int ResourceId { get; set; }

        public int Status { get; set; }
    }
}
