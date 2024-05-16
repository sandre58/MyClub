// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;

namespace MyClub.Domain
{
    public class NameEntity : AuditableEntity, ISimilar
    {
        private string _name = string.Empty;
        private string _shortName = string.Empty;

        protected NameEntity(string name, string? shortName = null, Guid? id = null) : base(id)
        {
            Name = name;
            ShortName = shortName ?? name.GetInitials();
        }

        public string Name
        {
            get => _name;
            set => _name = value.IsRequiredOrThrow();
        }

        public string ShortName
        {
            get => _shortName;
            set => _shortName = value.IsRequiredOrThrow();
        }

        public override int CompareTo(object? obj) => obj is NameEntity other ? string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase) : 1;

        public virtual bool IsSimilar(object? obj) => obj is NameEntity other && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);

        public override string ToString() => Name;
    }
}
