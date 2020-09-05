﻿using SW.PrimitiveTypes;
using System;

namespace SW.Infolink.Domain
{
    class XchangeResult : BaseEntity<string>
    {
        private XchangeResult()
        {
        }

        public XchangeResult(string xchangeId, string responseXchangeId = null, string exception = null)
        {
            Id = Guid.NewGuid().ToString("N");
            Success = exception == null;
            Exception = exception;
            XchangeId = xchangeId;
            Success = true;
            FinishedOn = DateTime.Now;
            ResponseXchangeId = responseXchangeId;
            Events.Add(new XchangeResultCreatedEvent
            {
                Id = Id
            });
        }

        public string XchangeId { get; private set; }
        public bool Success { get; private set; }
        public string Exception { get; private set; }
        public DateTime FinishedOn { get; private set; }
        public string ResponseXchangeId { get; private set; }
    }
}