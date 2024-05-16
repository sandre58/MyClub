// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.UserContext.Application.Dtos
{
    public class UserDto
    {
        public string? DisplayName { get; set; }

        public string? Email { get; set; }

        public byte[]? Image { get; set; }
    }
}
