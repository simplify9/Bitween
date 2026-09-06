using System.Threading.Tasks;
using SW.Serverless.Sdk;

namespace SW.Bitween.SampleResidentHandler;

static class Program
{
    // The only line that differs from the classic sample handler.
    static Task Main() => Runner.RunResident(new Handler());
}
