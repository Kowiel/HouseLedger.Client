using HouseLedger.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseLedger.Server.Data
{
    public class SessionDbContext : DbContext
    {
        public SessionDbContext(DbContextOptions<SessionDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserSession> UserSessions => Set<UserSession>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserSession>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.UserId);

                entity.HasIndex(x => x.RefreshTokenHash)
                    .IsUnique();
            });
        }
    }
}