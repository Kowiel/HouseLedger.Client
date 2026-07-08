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
    }


}
