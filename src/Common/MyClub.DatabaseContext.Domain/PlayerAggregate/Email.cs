// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Linq;

namespace MyClub.DatabaseContext.Domain.PlayerAggregate
{
    public class Email : Entity
    {
        public Email() { }

        public Email(Guid id) : base(id) { }

        public string? Label { get; set; }

        public bool Default { get; set; }

        public string? Value { get; set; }

        public Player? Player { get; set; }

        public override string? ToString() => Value;

        public override bool IsSimilar(object? obj) => obj is Email other && Value == other.Value && Player == other.Player;

        public override void SetFrom(object? from)
        {
            if (from is Email email)
            {
                Value = email.Value;
                Label = email.Label;
                Default = email.Default;
            }
        }
    }
}
