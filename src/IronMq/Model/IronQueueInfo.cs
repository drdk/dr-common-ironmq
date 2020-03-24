using DR.Common.IronMq.Kermit;

namespace DR.Common.IronMq.Model
{
    public class IronQueueInfo : IIronQueueInfo
    {
        public string Name { get; set; }
        public int? Size { get; set; }
        public string Id { get; set; }
        public string ProjectId { get; set; }
        public int? MessageExpiration { get; set; }
        public int? MessageTimeout { get; set; }
        public int? TotalMessages { get; set; }
    }
}
