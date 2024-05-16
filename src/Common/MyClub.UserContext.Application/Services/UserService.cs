// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.UserContext.Application.Dtos;
using MyClub.UserContext.Domain.UserAggregate;

namespace MyClub.UserContext.Application.Services
{
    public class UserService(IUserRepository userRepository)
    {
        private readonly IUserRepository _userRepository = userRepository;

        public void Save(UserDto dto)
        {
            var user = _userRepository.GetCurrent();
            user.DisplayName = dto.DisplayName;
            user.Email = dto.Email;
            user.Image = dto.Image;

            _userRepository.Save();
        }
    }
}
