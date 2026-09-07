using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SW.Bitween.Domain.DataSources;
using SW.Bitween.Model;
using SW.PrimitiveTypes;
using SW.Serverless.Resident;

namespace SW.Bitween.Resources.DataSources;

/// <summary>
/// Asks the RUNNING adapter what it can see on the broker right now.
///
/// Deliberately not the same shape as <see cref="Test"/>. A test starts a throwaway instance,
/// because its whole job is to answer "would these settings work" before anything depends on them.
/// Discover and GetStats are questions about the connection that is actually serving traffic, so
/// they go to that instance — starting a second one would answer about a connection nobody is
/// using, and on a broker that authorises per-connection it might not even answer the same way.
///
/// Both are read-only: neither consumes, acknowledges or publishes anything.
/// </summary>
[HandlerName("inspect")]
public class Inspect(BitweenDbContext dbContext, RequestContext requestContext,
    IResidentAdapterHost adapters = null) : ICommandHandler<int, DataSourceInspectRequest, object>
{
    /// <summary>
    /// The commands this endpoint will relay. An allow-list rather than a passthrough: the
    /// adapter also exposes Publish, which writes to the customer's broker, and that is not
    /// something a View-level read should be able to reach by naming it in a request body.
    /// </summary>
    private static readonly string[] Allowed = ["Discover", "GetStats"];

    public async Task<object> Handle(int key, DataSourceInspectRequest request)
    {
        await requestContext.EnsurePermission(dbContext, Model.Permissions.DataSources.View);

        var command = request?.Command ?? "Discover";
        if (!Allowed.Contains(command, StringComparer.OrdinalIgnoreCase))
            throw new SWException(
                $"'{command}' is not something this endpoint relays. Allowed: {string.Join(", ", Allowed)}.");

        var exists = await dbContext.Set<DataSource>().AsNoTracking().AnyAsync(d => d.Id == key);
        if (!exists)
            throw new SWNotFoundException($"DataSource with id '{key}' was not found");

        var instance = adapters?.Describe()
            .FirstOrDefault(h => h.InstanceKey == key.ToString());

        if (instance == null)
            return new DataSourceInspectResult
            {
                Ran = false,
                // A broker connection is exclusive, so "not here" is the normal answer on every
                // node but one — not a fault, and worth saying in those words.
                Error = "This node is not running the adapter for this data source, so it has "
                      + "nothing to ask. A broker connection is exclusive: only the node holding "
                      + "it can answer.",
            };

        try
        {
            var live = adapters.Get(instance.AdapterId, instance.InstanceKey);
            if (live == null)
                return new DataSourceInspectResult { Ran = false, Error = "The adapter went away." };

            var raw = await live.InvokeAsync<JObject>(command, timeoutSeconds: 30);
            return new DataSourceInspectResult
            {
                Ran = true,
                Command = command,
                Result = raw?.ToString(Newtonsoft.Json.Formatting.Indented) ?? "",
            };
        }
        catch (Exception ex)
        {
            // A command that fails is an answer about the broker, not a server error.
            return new DataSourceInspectResult { Ran = false, Command = command, Error = ex.Message };
        }
    }
}
