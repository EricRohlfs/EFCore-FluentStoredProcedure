using Microsoft.EntityFrameworkCore;

namespace Tests.Extensions.ProcedureBuilderTests.EfContexts
{
    public class SaltWaterFishingContext : DbContext
    {
        public SaltWaterFishingContext(DbContextOptions<SaltWaterFishingContext> options)
            : base(options)
        {
        }

    }
}