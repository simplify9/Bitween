using System;
using System.Collections.Generic;
using SW.PrimitiveTypes;

namespace SW.Bitween.Domain.Gateway;

public class BusGateway : BaseEntity, IAudited
{
    public string Name { get; set; }
    public int DocumentId { get; set; }

    /// <summary>
    /// Turns the gateway off without deleting it — its routes stop being offered the
    /// message. See <see cref="ApiGateway.Inactive"/>.
    /// </summary>
    public bool Inactive { get; set; }

    /// <summary>
    /// Null means the INTERNAL bus — the only behaviour that existed before, and still the
    /// default, so every gateway already in a database keeps working with no migration of data.
    /// Set it and this gateway is fed by an external broker through a resident adapter instead.
    /// </summary>
    public int? DataSourceId { get; set; }
    public DataSources.DataSource DataSource { get; set; }

    /// <summary>
    /// Which queue, topic or subscription on that data source feeds this gateway. Meaningless for
    /// the internal bus, where the Document's own BusMessageTypeName does the routing.
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// Per-gateway overrides handed to the adapter alongside the data source's own properties —
    /// prefetch, consumer group, visibility timeout. Connection settings belong on the DataSource;
    /// these are about this one subscription to it.
    /// </summary>
    public Dictionary<string, string> EndpointProperties { get; set; } = new();

    public ICollection<BusGatewayRoute> Routes { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public string ModifiedBy { get; set; }
}
