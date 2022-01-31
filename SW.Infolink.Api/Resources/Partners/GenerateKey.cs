using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SW.Infolink.Resources.Partners
{
    [Returns(Type = typeof(string),StatusCode = 200)]
    [HandlerName("generatekey") ]
    class GenerateKey : IQueryHandler
    {
        public Task<object> Handle()
        {
            return Task.FromResult((object)Guid.NewGuid().ToString("N"));
        }
    }
}
