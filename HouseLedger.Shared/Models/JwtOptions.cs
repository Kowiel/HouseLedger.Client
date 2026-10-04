using System;
using System.Collections.Generic;
using System.Text;

namespace HouseLedger.Shared.Models
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string SigningKey { get; set; } = string.Empty;

        public int AccessTokenLifetimeMinutes { get; set; } = 15;
    }
}
