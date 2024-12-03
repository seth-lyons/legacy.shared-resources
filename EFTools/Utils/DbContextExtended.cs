using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharedResources.Utils
{
    // To Scaffold DB: Scaffold-DbContext "Server={host};Initial Catalog={database};Persist Security Info=False;User ID={username};Password={password};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Contexts -FORCE
    public class DbContextExtended : DbContext
    {
        public DbContextExtended(DbContextOptions<DbContextExtended> options) : base(options)
        {
        }

        public override int SaveChanges()
        {
            UpdateTrackedEntities();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTrackedEntities();
            return base.SaveChangesAsync();
        }

        private void UpdateTrackedEntities()
        {
            var utcnow = DateTime.UtcNow;
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is ITrackedEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                ((ITrackedEntity)entry.Entity).Modified = utcnow;
                if (entry.State == EntityState.Added)
                    ((ITrackedEntity)entry.Entity).Created = utcnow;
            }
        }
    }
}
