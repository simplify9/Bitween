using SW.Bitween.Domain;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SW.Bitween
{
    class SubscribersByDocument(int DocumentId, bool Inactive = false) : ISpecification<Subscription>
    {
        public Expression<Func<Subscription, bool>> Criteria { get; } = e => e.DocumentId == DocumentId && e.Inactive == Inactive;
    }
}
