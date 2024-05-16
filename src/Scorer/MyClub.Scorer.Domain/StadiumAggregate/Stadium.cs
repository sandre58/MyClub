// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Domain.StadiumAggregate
{
    public class Stadium : AuditableEntity
    {
        private string _name = string.Empty;

        public Stadium(string name, Ground ground, Guid? id = null) : base(id)
        {
            Name = name;
            Ground = ground;
        }

        public virtual string Name
        {
            get => _name;
            set => _name = value.IsRequiredOrThrow();
        }

        public Ground Ground { get; set; }

        public Address? Address { get; set; }

        public override string ToString() => string.Join(", ", new[] { Name, Address?.City }.NotNull());
    }
}
