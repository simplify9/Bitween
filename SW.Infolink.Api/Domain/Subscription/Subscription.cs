﻿using SW.EfCoreExtensions;
using SW.Infolink.Model;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;


namespace SW.Infolink.Domain
{
    public class Subscription : BaseEntity
    {
        public Subscription()
        {
        }

        //receiving
        public Subscription(string name, int documentId) : this(name, documentId, SubscriptionType.Receiving)
        {
            Inactive = true;
        }

        //aggregation
        public Subscription(string name, int aggregationFor, int partnerId) : this(name, Document.AggregationDocumentId,
            SubscriptionType.Aggregation, partnerId, aggregationFor)
        {
            Inactive = true;
        }

        //apiresult or filter
        public Subscription(string name, int documentId, SubscriptionType type, int partnerId) : this(name, documentId,
            type, partnerId, null)
        {
            Inactive = true;
            if (!(type == SubscriptionType.ApiCall || type == SubscriptionType.Internal))
                throw new ArgumentException();
        }

        private Subscription(string name, int documentId, SubscriptionType type, int? partnerId = null,
            int? aggregationForId = null, bool temporary = false)
        {
            Inactive = true;
            AggregationForId = aggregationForId;
            PartnerId = partnerId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DocumentId = documentId;
            Type = type;
            _Schedules = new HashSet<Schedule>();
            HandlerProperties = new Dictionary<string, string>();
            MapperProperties = new Dictionary<string, string>();
            ReceiverProperties = new Dictionary<string, string>();
            ValidatorProperties = new Dictionary<string, string>();
            DocumentFilter = new Dictionary<string, string>();
            Temporary = temporary;
        }

        public string Name { get; set; }
        public int DocumentId { get; private set; }
        public SubscriptionType Type { get; private set; }
        public int? PartnerId { get; private set; }
        public int? CategoryId { get; set; }
        public SubscriptionCategory Category { get; set; }
        public bool Temporary { get; private set; }
        public DateTime? PausedOn { get; private set; }
        public string ValidatorId { get; set; }
        public string HandlerId { get; set; }
        public string ReceiverId { get; set; }

        public string MapperId { get; set; }
        public IReadOnlyDictionary<string, string> ValidatorProperties { get; private set; }
        public IReadOnlyDictionary<string, string> HandlerProperties { get; private set; }
        public IReadOnlyDictionary<string, string> MapperProperties { get; private set; }
        public IReadOnlyDictionary<string, string> ReceiverProperties { get; private set; }
        public IReadOnlyDictionary<string, string> DocumentFilter { get; private set; }

        public IPropertyMatchSpecification MatchExpression { get; private set; }
        public bool IsRunning { get; set; }
        public bool Inactive { get; set; }
        public int? ResponseSubscriptionId { get; set; }
        public string ResponseMessageTypeName { get; set; }
        public int? AggregationForId { get; private set; }
        public XchangeFileType AggregationTarget { get; set; }
        public DateTime? AggregateOn { get; private set; }
        public int ConsecutiveFailures { get; private set; }
        public string LastException { get; private set; }

        readonly HashSet<Schedule> _Schedules;
        public IReadOnlyCollection<Schedule> Schedules => _Schedules;
        public DateTime? ReceiveOn { get; private set; }


        public void SetAggregateNow()
        {
            AggregateOn = DateTime.UtcNow.AddMinutes(-1);
        }


        public void SetDictionaries(
            IReadOnlyDictionary<string, string> handler,
            IReadOnlyDictionary<string, string> mapper,
            IReadOnlyDictionary<string, string> receiver,
            IReadOnlyDictionary<string, string> document,
            IReadOnlyDictionary<string, string> validator
        )
        {
            HandlerProperties = handler;
            MapperProperties = mapper;
            ReceiverProperties = receiver;
            ValidatorProperties = validator;
            DocumentFilter = document;
        }
        public void SetSchedules(IEnumerable<Schedule> schedules = null)
        {
            if (Type == SubscriptionType.Receiving)
            {
                if (schedules != null) _Schedules.Update(schedules);
                ReceiveOn = _Schedules.Next() ?? throw new InfolinkException("Invalid schedule.");
            }
            else if (Type == SubscriptionType.Aggregation)
            {
                if (schedules != null) _Schedules.Update(schedules);
                AggregateOn = _Schedules.Next() ?? throw new InfolinkException("Invalid schedule.");
            }
        }

        public void SetReceiveNow()
        {
            ReceiveOn = DateTime.UtcNow.AddMinutes(-1);
        }

        public void SetHealth(string exception = null)
        {
            if (exception == null)
            {
                ConsecutiveFailures = 0;
                LastException = null;
                return;
            }

            ConsecutiveFailures += 1;
            LastException = exception;
        }

        public void SetMatchExpression(IPropertyMatchSpecification matchExpression)
        {
            MatchExpression = matchExpression;
        }

        public void Pause()
        {
            PausedOn = DateTime.Now;
        }

        public void UnPause()
        {
            PausedOn = null;
            Events.Add(new SubscriptionUnpausedEvent
            {
                Id = Id
            });
        }
    }
}