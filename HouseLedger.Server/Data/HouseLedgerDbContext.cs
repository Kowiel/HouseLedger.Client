using HouseLedger.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HouseLedger.Server.Data
{
    public class HouseLedgerDbContext: IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
    {
        public HouseLedgerDbContext(DbContextOptions<HouseLedgerDbContext> options): base(options)
        {

        }

        public DbSet<Tool> Tools => Set<Tool>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Contact> Contacts => Set<Contact>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AppUser>()
                .HasIndex(user => user.NormalizedEmail)
                .IsUnique()
                .HasDatabaseName("EmailIndex");

            builder.Entity<IdentityRole<Guid>>()
                .Property(role => role.Id)
                .HasValueGenerator<UuidV7ValueGenerator>();

            builder.Entity<AppUser>()
                .Property(user => user.Id)
                .HasValueGenerator<UuidV7ValueGenerator>();

            builder.Entity<Contact>()
                .Property(contact => contact.Id)
                .HasValueGenerator<UuidV7ValueGenerator>();

            builder.Entity<Room>()
                .Property(room => room.Id)
                .HasValueGenerator<UuidV7ValueGenerator>();

            builder.Entity<Tool>()
                .Property(tool => tool.Id)
                .HasValueGenerator<UuidV7ValueGenerator>();
        }
    }


}
