using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace HouseLedger.Shared.Models
{
    public class AppUser : IdentityUser<Guid>
    {
        // AppUser inherits these built-in fields from IdentityUser:
        // string Id
        // string? UserName
        // string? NormalizedUserName
        // string? Email
        // string? NormalizedEmail
        // bool EmailConfirmed
        // string? PasswordHash
        // string? SecurityStamp
        // string? ConcurrencyStamp
        // string? PhoneNumber
        // bool PhoneNumberConfirmed
        // bool TwoFactorEnabled
        // DateTimeOffset? LockoutEnd
        // bool LockoutEnabled
        // int AccessFailedCount

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? DisplayName { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

    }
}
