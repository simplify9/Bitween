﻿using SW.Infolink.Domain;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SW.Infolink
{
    class DueReceivers : ISpecification<Subscription>
    {
        public DueReceivers(DateTime? asOf = null)
        {
            if (asOf == null) asOf = DateTime.UtcNow;

            Criteria = e => e.ReceiveOn < asOf
                || e.ReceiveOn == null;
        }

        public Expression<Func<Subscription, bool>> Criteria { get; }

    }
}
