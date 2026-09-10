using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SW.Bitween.IntegrationTests.Fixtures;
using SW.Bitween.Model;
using SW.PrimitiveTypes;
using Xunit;

namespace SW.Bitween.IntegrationTests.Tests;

/// <summary>
/// Describing an adapter — which startup properties it expects — and listing a whole kind of them
/// at once.
/// </summary>
/// <remarks>
/// The published adapter here is the one the fixture uploads to local storage, so these are the
/// only tests that go through the expensive half: downloading the package and running it in a child
/// process to ask what it wants. The native adapters answer by reflection and cost nothing.
/// </remarks>
[Collection("Bitween")]
public class AdapterCatalogTests
{
    private const string PublishedAdapter = "sw.bitween.sampleconfigurableadapter";

    private readonly BitweenFixture _fixture;

    public AdapterCatalogTests(BitweenFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<IDictionary<string, StartupValue>> Describe(string adapterId)
    {
        await using var scope = _fixture.CreateScope();
        scope.Superuser();
        return await scope.ServiceProvider
            .GetRequiredService<AdapterStartupValues>()
            .Describe(adapterId);
    }

    private async Task Forget(string adapterId)
    {
        await using var scope = _fixture.CreateScope();
        scope.ServiceProvider.GetRequiredService<AdapterStartupValues>().Forget(adapterId);
    }

    [Fact]
    public async Task A_published_adapter_reports_the_values_it_expects()
    {
        var described = await Describe(PublishedAdapter);

        Assert.Equal(
            new[] { "DelayMs", "ErrorMessage", "OutputData", "SimulateError" },
            described.Keys.OrderBy(k => k).ToArray());
    }

    /// <summary>
    /// The same instance comes back on the second ask, which is only possible from the cache —
    /// running the adapter again would have built a new dictionary.
    /// </summary>
    /// <remarks>
    /// Asked from two separate scopes, because a request gets its own scope and caching that only
    /// lasted the length of one would leave the catalogue exactly as slow as it was.
    /// </remarks>
    [Fact]
    public async Task Describing_a_published_adapter_twice_only_runs_it_once()
    {
        // From a known-cold cache, so the first ask is the one that runs the adapter however the
        // other tests in this collection happened to be ordered.
        await Forget(PublishedAdapter);

        var first = await Describe(PublishedAdapter);
        var second = await Describe(PublishedAdapter);

        Assert.Same(first, second);
    }

    /// <summary>
    /// Several requests arriving together on a cold cache still only run the adapter once.
    /// </summary>
    /// <remarks>
    /// They all come back with the same instance, which is only possible if one of them did the
    /// work and the rest waited for it — each separate run of the adapter builds its own dictionary.
    /// </remarks>
    [Fact]
    public async Task Describing_the_same_adapter_from_several_requests_at_once_runs_it_once()
    {
        await Forget(PublishedAdapter);

        var asks = Enumerable.Range(0, 8).Select(_ => Task.Run(() => Describe(PublishedAdapter)));
        var results = await Task.WhenAll(asks);

        Assert.All(results, r => Assert.Same(results[0], r));
    }

    /// <summary>
    /// One instance is shared by every request that asks, so editing it would change what the next
    /// request is told the adapter expects.
    /// </summary>
    [Fact]
    public async Task A_cached_description_cannot_be_edited()
    {
        var described = await Describe(PublishedAdapter);

        Assert.Throws<System.NotSupportedException>(() => described.Remove("DelayMs"));
    }

    /// <summary>
    /// The catalogue answers for a whole kind in one call, properties included — the point of it
    /// being that a caller drawing a form per adapter no longer asks once per adapter.
    /// </summary>
    /// <remarks>
    /// Only the native handlers are asserted. The fixture's published adapters are uploaded under
    /// their own key rather than under the <c>infolink6.handlers</c> prefix the listing reads, so
    /// they are not part of any kind's list — <see cref="Describing_a_published_adapter_twice_only_runs_it_once"/>
    /// is what covers the published path.
    /// </remarks>
    [Fact]
    public async Task The_catalog_lists_a_kind_with_every_adapters_properties()
    {
        await using var scope = _fixture.CreateScope();
        scope.Superuser();
        var handler = ActivatorUtilities
            .CreateInstance<Resources.Adapters.Catalog>(scope.ServiceProvider);

        var result = await handler.Handle(new AdapterSearchRequest { Prefix = "handlers" });

        // The handler returns anonymous types; going through JSON reads them the way the browser
        // does rather than through reflection.
        var rows = JArray.Parse(JsonConvert.SerializeObject(result));

        Assert.NotEmpty(rows);
        Assert.All(rows, row => Assert.NotNull(row["StartupValues"]));

        // The properties arrive with the list rather than needing a request each, which is the
        // whole reason this endpoint exists.
        var smtp = rows.Single(r => r["Key"]!.ToString() == "NativeSmtpHandler");
        Assert.True(smtp["Native"]!.Value<bool>());
        Assert.NotEmpty(smtp["StartupValues"]!.Children<JProperty>());
    }
}
