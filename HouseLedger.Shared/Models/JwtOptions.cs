using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace HouseLedger.Shared.Models
{
    public class JwtOptions
    {
        [Required]
        public string Issuer { get; set; } = string.Empty;
        [Required]
        public string Audience { get; set; } = string.Empty;
        [Required]
        public string SigningKey { get; set; } = string.Empty;

        public int AccessTokenLifetimeMinutes { get; set; } = 15;
    }
}
