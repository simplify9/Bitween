﻿using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using SW.Infolink.Model;

namespace SW.Infolink.Domain
{
    public class Xchange : BaseEntity<string>
    {
        private Xchange()
        {
        }

        public Xchange(int documentId, XchangeFile file, string[] references = null, SubscriptionType subscriptionType = SubscriptionType.Internal, string correlationId = null)
        {
            Id = Guid.NewGuid().ToString("N"); 
            DocumentId = documentId;
            References = references ?? new string[] { };
            InputName = file.Filename;
            InputSize = file.Data.Length;
            InputHash = file.Hash;
            InputContentType = file.ContentType;
            StartedOn = DateTime.UtcNow;
            CorrelationId = correlationId;

            XchangeCreatedEvent xchangeEvent = subscriptionType switch
            {
                //break;
                SubscriptionType.Internal => new InternalXchangeCreatedEvent(),
                SubscriptionType.ApiCall => new ApiXchangeCreatedEvent(),
                SubscriptionType.Receiving => new ReceivingXchangeCreatedEvent(),
                SubscriptionType.Aggregation => new AggregateXchangeCreatedEvent(),
                _ => throw new ArgumentOutOfRangeException(nameof(subscriptionType), subscriptionType, null)
            };

            xchangeEvent.Id = Id;
            Events.Add(xchangeEvent);
        }

        public Xchange(Subscription subscription, XchangeFile file, string[] references = null, string correlationId = null) : 
            this(subscription.DocumentId, file, references, subscription.Type)
        {
            SubscriptionId = subscription.Id;
            MapperId = subscription.MapperId;
            HandlerId = subscription.HandlerId;
            MapperProperties = subscription.MapperProperties;
            HandlerProperties = subscription.HandlerProperties;
            ResponseSubscriptionId = subscription.ResponseSubscriptionId;
            ResponseMessageTypeName = subscription.ResponseMessageTypeName;
            CorrelationId = correlationId;
        }

        //retry xchange
        public Xchange(Xchange xchange, XchangeFile file) : 
            this(xchange.DocumentId, file, xchange.References)
        {
            SubscriptionId = xchange.SubscriptionId;
            MapperId = xchange.MapperId;
            HandlerId = xchange.HandlerId;
            MapperProperties = xchange.MapperProperties;
            HandlerProperties = xchange.HandlerProperties;
            ResponseSubscriptionId = xchange.ResponseSubscriptionId;
            RetryFor = xchange.Id;
            CorrelationId = xchange.CorrelationId;
        }
        //retry with reset subscription properties
        public Xchange(Subscription subscription, Xchange xchange, XchangeFile file) : 
            this(xchange.DocumentId, file, xchange.References)
        {
            SubscriptionId = xchange.SubscriptionId;
            MapperId = subscription.MapperId;
            HandlerId = subscription.HandlerId;
            MapperProperties = subscription.MapperProperties;
            HandlerProperties = subscription.HandlerProperties;
            ResponseSubscriptionId = subscription.ResponseSubscriptionId;
            RetryFor = xchange.Id;
            CorrelationId = xchange.CorrelationId;
        }

        public int? SubscriptionId { get; private set; }
        public int DocumentId { get; private set; }
        public string HandlerId { get; private set; }
        public string MapperId { get; private set; }
        public IReadOnlyDictionary<string, string> HandlerProperties { get; private set; }
        public IReadOnlyDictionary<string, string> MapperProperties { get; private set; }
        public string[] References { get; private set; }
        public DateTime StartedOn { get; private set; }
        public string InputName { get; private set; }
        public int InputSize { get; private set; }
        public string InputHash { get; private set; }
        public string InputContentType { get; private set; }
        public int? ResponseSubscriptionId { get; private set; }
        public string ResponseMessageTypeName { get; private set; }

        public string RetryFor { get; private set; }
        public string CorrelationId { get; set; }

    }
}
