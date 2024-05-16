// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.UserContext.Domain.UserAggregate
{
    public interface IUserRepository
    {
        User GetCurrent();

        void Save();
    }
}
