using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace DR.Common.IronMq.Kermit.Model
{
    public class CommonMessage<TMessage> : IQueueMessage<string, string, DateTimeOffset>
    {
        public DateTimeOffset Timestamp { get; set; }
        public virtual TMessage Type { get; set; }
        [JsonIgnore]
        public virtual string Identifier { get; }
        [JsonIgnore]
        public virtual string CorrelationId => Identifier;
        [JsonIgnore]
        public virtual DateTimeOffset Version => Timestamp;

        [DefaultValue(1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public virtual int SchemaVersion { get; set; } = 1;

        [JsonIgnore]
        [DefaultValue("")]
        public virtual string CorrelationType => string.Empty;
    }
}