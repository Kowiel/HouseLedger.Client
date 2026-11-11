using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseLedger.Shared.Models
{
    public class UserSession
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public Guid UserId { get; set; }

        public string RefreshTokenHash { get; set; } = string.Empty;

        [NotMapped]
        public string RefreshToken { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime LastActivityAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public bool IsRevoked { get; set; }
    }
}
