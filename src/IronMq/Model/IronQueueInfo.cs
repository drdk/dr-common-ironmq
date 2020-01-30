namespace DR.Common.IronMq.Model
{
    public class IronQueueInfo
    {
        public string Name { get; set; }
        public int? Size { get; set; }
        //public List<Alert> Alerts { get; set; }
        //public QueueDeadletterInfo QueueDeadletterInfo { get; set; }
        public string Id { get; set; }
        public string ProjectId { get; set; }
        public int? MessageExpiration { get; set; }
        public int? MessageTimeout { get; set; }
        //public PushInfo PushInfo { get; set; }
        //public PushType PushType { get; set; }
        public int? TotalMessages { get; set; }
    }
}
