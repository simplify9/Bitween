using Microsoft.Extensions.DependencyInjection;
using SW.Serverless.Sdk.Hosting;
using System.Threading.Tasks;

namespace SW.Bitween.Adapters.Db.MySql;

static class Program
{
    static Task Main() => AdapterHost.CreateBuilder()
        .ConfigureServices((configuration, services) => services.Configure<MySqlOptions>(configuration))
        .Build<MySqlDbAdapter>()
        .RunResidentAsync();
}
