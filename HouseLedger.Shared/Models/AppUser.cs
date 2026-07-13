using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace HouseLedger.Shared.Models
{
    public class AppUser : IdentityUser<Guid>
    {
        //We asume that the UserName is Unique and cant be changed. So it will be an extra key for the user. We will use it to identify the user in the system.
        //We will still use the Id as the primary key for the user, but we will use the UserName as a unique identifier for the user in the system during updates so to not
        // send the id in the request body. This is because the id is a guid and it is not user friendly. 

        // AppUser inherits these built-in fields from IdentityUser:
        // Guid Id
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
