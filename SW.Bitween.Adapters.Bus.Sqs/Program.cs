using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SW.Serverless.Sdk.Hosting;
using System.Threading.Tasks;

namespace SW.Bitween.Adapters.Bus.Sqs;

static class Program
{
    static Task Main() => AdapterHost.CreateBuilder()
        .ConfigureServices((configuration, services) => services.Configure<SqsOptions>(configuration))
        .Build<SqsBusHandler>()
        .RunResidentAsync();
}
