using DR.Common.IronMq.Kermit;

namespace DR.Common.IronMq.Model
{
    public interface IIronQueueInfo : IQueueInfo
    {
        //public List<Alert> Alerts { get; set; }
        //public QueueDeadletterInfo QueueDeadletterInfo { get; set; }
        string Id { get; set; }
        string ProjectId { get; set; }
        int? MessageExpiration { get; set; }
        int? MessageTimeout { get; set; }
        //public PushInfo PushInfo { get; set; }
        //public PushType PushType { get; set; }
        int? TotalMessages { get; set; }
    }
}