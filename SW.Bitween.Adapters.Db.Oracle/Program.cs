using Microsoft.Extensions.DependencyInjection;
using SW.Serverless.Sdk.Hosting;
using System.Threading.Tasks;

namespace SW.Bitween.Adapters.Db.Oracle;

static class Program
{
    static Task Main() => AdapterHost.CreateBuilder()
        .ConfigureServices((configuration, services) => services.Configure<OracleOptions>(configuration))
        .Build<OracleDbAdapter>()
        .RunResidentAsync();
}
