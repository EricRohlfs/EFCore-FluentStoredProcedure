using Microsoft.EntityFrameworkCore;

namespace Tests.Extensions.ProcedureBuilderTests.EfContexts
{
    public class BassFishingContext : DbContext
    {
        public BassFishingContext(DbContextOptions<BassFishingContext> options)
            : base(options)
        {
        }
        public BassFishingContext() : base()
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("Bass");
        }
    }
}