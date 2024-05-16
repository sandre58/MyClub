// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Runtime.Versioning;
using System.Security.Principal;
using MyClub.CrossCutting.Localization;
using MyClub.UserContext.Domain.UserAggregate;
using MyNet.Utilities.Authentication.Windows;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.IO.Registry;

namespace MyClub.UserContext.Infrastructure.Authentication.Registry
{
    [SupportedOSPlatform("windows")]
    public class RegistryAuthenticationService(string registryPath) : WindowsAuthenticationService<User>, IUserAuthenticationService, IUserRepository
    {
        public static readonly User Anonymous = new(new GenericIdentity(string.Empty), [])
        {
            DisplayName = MyClubResources.Anonymous
        };

        private readonly string _registryPath = registryPath;
        private readonly RegistryService _registryService = new();

        public User GetCurrent() => CurrentPrincipal;

        protected override User CreatePrincipal(IIdentity identity)
        {
            var existingRegistry = _registryService.Get<UserRegistry>(_registryPath, identity.GetName());

            return new User(identity, [])
            {
                DisplayName = existingRegistry?.Item?.DisplayName,
                Email = existingRegistry?.Item?.Email,
                Image = existingRegistry?.Item?.Image,
            };
        }

        public void Save()
        {
            if (IsAuthenticated && !string.IsNullOrEmpty(CurrentPrincipal.Name))
                _registryService.AddOrUpdate(new Registry<UserRegistry>(CurrentPrincipal.Identity.GetName(), _registryPath, new UserRegistry
                {
                    DisplayName = CurrentPrincipal?.DisplayName,
                    Email = CurrentPrincipal?.Email,
                    Image = CurrentPrincipal?.Image,
                    Name = CurrentPrincipal?.Name,
                }));
        }

        protected override User GetAnonymous() => Anonymous;

        private sealed class UserRegistry
        {
            public string? Name { get; set; }

            public string? DisplayName { get; set; }

            public string? Email { get; set; }

            public byte[]? Image { get; set; }
        }
    }
}
