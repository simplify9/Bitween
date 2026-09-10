using System;

namespace SW.Bitween.Model
{
    public class NotificationsSearch
    {
        public int Id { get; set; }
        public string XchangeId { get; set; } = null!;
        public string NotifierName { get; set; } = null!;
        public bool Success { get; set; }

        /// <summary>Null when the notification succeeded.</summary>
        public string? Exception { get; set; }
        public DateTime FinishedOn { get; set; }
    }
}