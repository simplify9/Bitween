using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;

namespace SW.Bitween.Model
{
    //public enum XchangeStatus
    //{
    //    Running,
    //    Failed,
    //    Succeded
    //}

    public enum XchangeFileType
    {
        Input,
        Output,
        Response
    }

    public class CreateXchange
    {
        public CreateXchangeOption Option { get; set; }
        public int? DocumentId { get; set; }
        public int? SubscriberId { get; set; }
        /// <summary>The message body. Required; the server rejects a create without it.</summary>
        public string Data { get; set; } = null!;
    }

    public enum CreateXchangeOption
    {
        DocumentId,
        SubscriberId
    }


    public class XchangeRetry
    {
        /// <summary>Optional note recorded against the retry.</summary>
        public string? Reason { get; set; }
        public bool Reset { get; set; }
    }

    public class XchangeBulkRetry : XchangeRetry
    {
        public List<string> Ids { get; set; } = [];
    }

    public class XchangeGetResultResponse
    {
        public bool Success { get; set; }
        /// <summary>Each is null when that file was never produced.</summary>
        public string? InputUri { get; set; }

        public string? OutputUri { get; set; }

        public string? ResponseUri { get; set; }
    }

    //public class XchangeUnderProcessing : IUnderProcessing
    //{
    //    public string Uri { get; set; }
    //}

    public class XchangeRow
    {
        public string Id { get; set; } = null!;
        public int? SubscriptionId { get; set; }

        /// <summary>Null alongside a null SubscriptionId.</summary>
        public string? SubscriptionName { get; set; }

        public int DocumentId { get; set; }
        public string DocumentName { get; set; } = null!;

        /// <summary>Null when the subscription skips that stage.</summary>
        public string? HandlerId { get; set; }

        public string? MapperId { get; set; }

        public string[] References { get; set; } = [];
        public bool? Status { get; set; }
        public int StatusFilter { get; set; }
        public string StatusString { get; set; } = null!;

        /// <summary>Null unless the exchange failed.</summary>
        public string? Exception { get; set; }
        public DateTime? DeliveredOn { get; set; }
        public DateTime? FinishedOn { get; set; }
        public DateTime? AggregatedOn { get; set; }
        public DateTime StartedOn { get; set; }
        /// <summary>Every file field below is null when that file was never produced.</summary>
        public string? InputFileName { get; set; }

        public int InputFileSize { get; set; }

        public string? InputFileHash { get; set; }

        public string? OutputFileName { get; set; }

        public string? ResponseFileName { get; set; }

        public string? InputUrl { get; set; }
        public string? OutputUrl { get; set; }
        public string? ResponseUrl { get; set; }
        public string? InputKey { get; set; }
        public string? OutputKey { get; set; }
        public string? ResponseKey { get; set; }

        /// <summary>Null while the exchange is still running.</summary>
        public string? Duration { get; set; }

        /// <summary>Null when the document type promotes nothing.</summary>
        public IDictionary<string, string>? PromotedProperties { get; set; }

        public string? PromotedPropertiesRaw { get; set; }

        /// <summary>The exchange this one retries; null for an original delivery.</summary>
        public string? RetryFor { get; set; }

        /// <summary>The aggregate this was rolled into; null when it was not.</summary>
        public string? AggregationXchangeId { get; set; }
        public bool? OutputBad { get; set; }
        public bool? ResponseBad { get; set; }
        public string CorrelationId { get; set; } = null!;
        public int? PartnerId { get; set; }
        public DateTime? ScheduledRetryOn { get; set; }

        /// <summary>Why the retry policy declined to schedule another attempt, when it declined.</summary>
        public string? RetryBlockedReason { get; set; }
    }
}