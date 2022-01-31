using SW.Infolink.Domain;
using SW.Infolink.Model;
using SW.PrimitiveTypes;
using System.Threading.Tasks;

namespace SW.Infolink.Resources.Partners
{
    [Returns(Type = typeof(int),StatusCode = 200)]
    class Create : ICommandHandler<PartnerCreate>
    {
        private readonly InfolinkDbContext dbContext;

        public Create(InfolinkDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        async public Task<object> Handle(PartnerCreate model)
        {
            var entity = new Partner(model.Name);
            dbContext.Add(entity);
            await dbContext.SaveChangesAsync();
            return entity.Id;
        }
    }
}
