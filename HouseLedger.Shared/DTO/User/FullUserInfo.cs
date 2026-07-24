using System;
using System.Collections.Generic;
using System.Text;

namespace HouseLedger.Shared.DTO.User
{
    public class FullUserInfo
    {
        Guid Id { get; set; }
        public string UserName { get; set; }
        public string NormalizedUserName { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? DisplayName { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
