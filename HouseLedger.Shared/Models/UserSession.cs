using System;
using System.Collections.Generic;
using System.Text;

namespace HouseLedger.Shared.Models
{
    public class UserSession
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string RefreshTokenHash { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime LastActivityAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public bool IsRevoked { get; set; }
    }
}
