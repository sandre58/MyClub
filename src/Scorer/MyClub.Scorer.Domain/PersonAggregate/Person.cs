// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Domain.PersonAggregate
{
    public class Person : AuditableEntity, IAggregateRoot
    {
        private string _lastName = null!;
        private string _firstName = null!;

        protected Person(string firstName, string lastName, Guid? id = null) : base(id)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public string LastName
        {
            get => _lastName;
            set => _lastName = value.IsRequiredOrThrow();
        }

        public string FirstName
        {
            get => _firstName;
            set => _firstName = value.IsRequiredOrThrow();
        }

        public Country? Country { get; set; }

        public byte[]? Photo { get; set; }

        public GenderType Gender { get; set; } = GenderType.Male;

        public string? LicenseNumber { get; set; }

        public string? Email { get; set; }

        public string GetInverseName() => string.Join(" ", LastName, FirstName);

        public string GetFullName() => string.Join(" ", FirstName, LastName);

        public override string? ToString() => GetInverseName();

        public override int CompareTo(object? obj) => obj is not Person other ? -1 : GetInverseName() != other.GetInverseName() ? string.Compare(GetInverseName(), other.GetInverseName(), StringComparison.OrdinalIgnoreCase) : base.CompareTo(obj);

        public bool IsSimilar(object? obj) => obj is Person person && GetInverseName().Equals(person.GetInverseName(), StringComparison.OrdinalIgnoreCase);
    }
}
