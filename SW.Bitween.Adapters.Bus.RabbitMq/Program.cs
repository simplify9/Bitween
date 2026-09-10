using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SW.Serverless.Sdk.Hosting;
using System.Threading.Tasks;

namespace SW.Bitween.Adapters.Bus.RabbitMq;

static class Program
{
    static Task Main() => AdapterHost.CreateBuilder()
        .ConfigureServices((configuration, services) => services.Configure<RabbitOptions>(configuration))
        .Build<RabbitBusHandler>()
        .RunResidentAsync();
}
