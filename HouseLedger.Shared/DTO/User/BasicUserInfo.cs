using System;
using System.Collections.Generic;
using System.Text;

namespace HouseLedger.Shared.DTO.User
{
    public class BasicUserInfo
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? DisplayName { get; set; }


        public static BasicUserInfo FromEntity(FullUserInfo userEntity)
        {
            return new BasicUserInfo
            {
                UserName = userEntity.UserName,
                Email = userEntity.Email,
                PhoneNumber = userEntity.PhoneNumber,
                FirstName = userEntity.FirstName,
                LastName = userEntity.LastName,
                DisplayName = userEntity.DisplayName
            };
        }
    }
}
