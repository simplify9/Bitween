﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SW.EfCoreExtensions;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SW.Infolink
{
    internal class BusService : IConsume
    {
        private const string MessageTypeNameToDocumentId = "MessageTypeNameToDocumentId";

        private readonly XchangeService xchangeService;
        private readonly IMemoryCache memoryCache;
        private readonly InfolinkDbContext dbContext;
        readonly IServiceScopeFactory ssf;
        private readonly RequestContext _requestContext;


        public BusService(XchangeService xchangeService, IMemoryCache memoryCache, InfolinkDbContext dbContext, IServiceScopeFactory ssf, RequestContext requestContext)
        {
            this.xchangeService = xchangeService;
            this.memoryCache = memoryCache;
            this.dbContext = dbContext;
            this.ssf = ssf;
            _requestContext = requestContext;
        }

        public async Task<IEnumerable<string>> GetMessageTypeNames()
        {
            var map = await GetMessageTypeNameToDocumentIdMap();
            return map.Keys;
        }
        public async Task Process(string messageTypeName, string message)
        {
            var map = await GetMessageTypeNameToDocumentIdMap();

            var xf = new XchangeFile(message);

            await xchangeService.SubmitFilterXchange(map[messageTypeName], xf,null, _requestContext.CorrelationId);
        }

        private async Task<IReadOnlyDictionary<string, int>> GetMessageTypeNameToDocumentIdMap()
        {
            if (memoryCache.TryGetValue(MessageTypeNameToDocumentId, out IReadOnlyDictionary<string, int> map))
                return map;

            map = (await dbContext.ListAsync(new BusEnabledDocuments())).ToDictionary(k => k.BusMessageTypeName, v => v.Id);
            return memoryCache.Set(MessageTypeNameToDocumentId, map);
        }
    }
}
