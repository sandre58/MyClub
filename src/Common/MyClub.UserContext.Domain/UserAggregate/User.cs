// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;
using MyNet.Utilities.Extensions;
using PropertyChanged;

namespace MyClub.UserContext.Domain.UserAggregate
{
    [AddINotifyPropertyChangedInterface]
    public class User : GenericPrincipal
    {
        public User(string name, string[] roles) : this(new GenericIdentity(name), roles) { }

        public User(IIdentity identity, string[] roles) : base(identity, roles)
        {
            Name = identity.GetName();
            Domain = identity.GetDomain();
        }

        public string Name { get; }

        public string Domain { get; }

        public string? DisplayName { get; set; }

        [AlsoNotifyFor(nameof(Name), nameof(DisplayName))]
        public string PreferredName => !string.IsNullOrEmpty(DisplayName) ? DisplayName : Name;

        public string? Email { get; set; }

        public byte[]? Image { get; set; }

        public override string ToString() => PreferredName;

        public override bool Equals(object? obj) => obj is User user && Name == user.Name;

        public override int GetHashCode() => Name.GetHashCode();
    }
}
