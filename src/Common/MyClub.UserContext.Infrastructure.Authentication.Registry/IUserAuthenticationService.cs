// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Utilities.Authentication;
using MyClub.UserContext.Domain.UserAggregate;

namespace MyClub.UserContext.Infrastructure.Authentication.Registry
{
    public interface IUserAuthenticationService : IAuthenticationService<User>
    {
    }
}
