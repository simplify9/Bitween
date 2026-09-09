using Microsoft.Extensions.DependencyInjection;
using SW.Serverless.Sdk.Hosting;
using System.Threading.Tasks;

namespace SW.Bitween.Adapters.Db.PostgreSql;

static class Program
{
    static Task Main() => AdapterHost.CreateBuilder()
        .ConfigureServices((configuration, services) => services.Configure<PostgreSqlOptions>(configuration))
        .Build<PostgreSqlDbAdapter>()
        .RunResidentAsync();
}
