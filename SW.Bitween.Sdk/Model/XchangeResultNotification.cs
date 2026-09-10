using System;

namespace SW.Bitween.Model
{
    public class XchangeResultNotification
    {
        public string Id { get; set; } = null!;
        public bool Success { get; set; }
        /// <summary>Null when the exchange succeeded.</summary>
        public string? Exception { get; set; }
        public DateTime FinishedOn { get; set; }
        public bool OutputBad { get; set; }
        public bool ResponseBad { get; set; }
        public string DocumentName { get; set; } = null!;
        public int DocumentId { get; set; }
        public int SubscriptionId { get; set; }
        public string SubscriptionName { get; set; } = null!;
        public string CorrelationId { get; set; } = null!;
        public DateTime StartedOn { get; set; }
    }
}