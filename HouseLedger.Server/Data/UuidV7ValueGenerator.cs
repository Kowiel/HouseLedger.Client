using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace HouseLedger.Server.Data
{
    public sealed class UuidV7ValueGenerator : ValueGenerator<Guid>
    {
        public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();

        public override bool GeneratesTemporaryValues => false;
    }
}
