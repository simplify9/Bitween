using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SW.Bitween.Model
{
    public enum DocumentFormat
    {
        Json = 0,
        Xml = 1
    }

    public class DocumentCreate : IName
    {
        /// <summary>Required; the server rejects a create without it.</summary>
        public string Code { get; set; } = null!;
        public DocumentFormat DocumentFormat { get; set; }
        /// <summary>Required; the server rejects a create without it.</summary>
        public string Name { get; set; } = null!;
        public bool BusEnabled { get; set; }
        /// <summary>Only meaningful when BusEnabled; null otherwise.</summary>
        public string? BusMessageTypeName { get; set; }
        public int DuplicateInterval { get; set; }

        public bool DisregardsUnfilteredMessages { get; set; }

        /// <summary>Carried on create too, so a new type arrives complete rather than
        /// needing a second save before it can be filtered on.</summary>
        /// <summary>
        /// Null leaves the existing promoted properties alone; an empty collection clears them.
        /// </summary>
        public ICollection<KeyAndValue>? PromotedProperties { get; set; }
    }

    public class DocumentUpdate : DocumentCreate
    {
        public int Id { get; set; }
    }

    public class DocumentRow : DocumentUpdate
    {
        /// <summary>
        /// How many subscriptions carry this information type. Counted here because the admin UI
        /// shows it in the list: computing it client-side meant downloading every subscription
        /// alongside every page of this list.
        /// </summary>
        public int UsedByCount { get; set; }
    }
}