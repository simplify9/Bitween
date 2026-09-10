using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SW.PrimitiveTypes;

namespace SW.Bitween.Services.Adapters;

/// <summary>
/// A packaged adapter spawned per invocation, answering over stdin/stdout and exiting.
///
/// The fallback runtime: anything not native and not marked resident is run this way, which is
/// what every uploaded adapter was before residency existed.
/// </summary>
public class ClassicAdapterRuntime(IServiceProvider serviceProvider) : IAdapterRuntime
{
    /// <summary>
    /// Claims everything. Registered last, so it only sees what the other runtimes declined.
    /// </summary>
    public Task<bool> CanRunAsync(string adapterId) => Task.FromResult(true);

    public async Task<IAdapterSession> BeginAsync(string adapterId, AdapterRole role,
        IDictionary<string, string> properties, string correlationId)
    {
        var serverless = serviceProvider.GetRequiredService<IServerlessService>();
        await serverless.StartAsync(adapterId, correlationId, properties);
        return new ClassicAdapterSession(serverless);
    }

    private sealed class ClassicAdapterSession(IServerlessService serverless) : IAdapterSession
    {
        public Task<TResult> InvokeAsync<TResult>(string method, object argument = null) =>
            serverless.InvokeAsync<TResult>(method, argument);

        public Task InvokeAsync(string method, object argument = null) =>
            serverless.InvokeAsync(method, argument);

        // The serverless service belongs to the DI scope and is disposed with it, which is how
        // this path has always worked.
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
