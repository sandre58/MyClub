// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.UserContext.Domain.UserAggregate;
using MyNet.Utilities.Mail;

namespace MyClub.Scorer.Wpf.Services.Factories
{
    public class UserEmailFactory(IUserRepository userRepository) : IEmailFactory
    {
        private readonly IUserRepository _userRepository = userRepository;

        public IEmail Create() => Email.From(_userRepository.GetCurrent().Email ?? string.Empty, _userRepository.GetCurrent().PreferredName);
    }
}
