using HouseLedger.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HouseLedger.Shared.DTO.User
{
    public class FullUserInfo
    {
        public Guid Id { get; set; }
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

        public static FullUserInfo FromEntity(AppUser userEntity)
        {
            return new FullUserInfo
            {
                Id = userEntity.Id,
                UserName = userEntity.UserName,
                NormalizedUserName = userEntity.NormalizedUserName,
                EmailConfirmed = userEntity.EmailConfirmed,
                Email = userEntity.Email,
                NormalizedEmail = userEntity.NormalizedEmail,
                PhoneNumber = userEntity.PhoneNumber,
                FirstName = userEntity.FirstName,
                LastName = userEntity.LastName,
                DisplayName = userEntity.DisplayName,
                CreatedDate = userEntity.CreatedDate
            };
        
        }
    }
}
