namespace DR.Common.IronMq.Kermit
{
    public interface IQueueInfo
    {
        string Name { get; set; }
        int? Size { get; set; }
    }
}