// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyClub.Domain.Services;

namespace MyClub.UserContext.Domain.UserAggregate
{
    public class AuditService(IUserRepository userRepository) : IAuditService
    {
        private readonly IUserRepository _userRepository = userRepository;

        public void New(IAuditable auditable) => auditable.MarkedAsCreated(DateTime.UtcNow, _userRepository.GetCurrent().PreferredName);

        public void Update(IAuditable auditable) => auditable.MarkedAsModified(DateTime.UtcNow, _userRepository.GetCurrent().PreferredName);
    }
}
