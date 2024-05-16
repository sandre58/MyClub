// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.DatabaseContext.Domain.PlayerAggregate
{
    public class Phone : Entity
    {
        public Phone() { }

        public Phone(Guid id) : base(id) { }

        public string? Label { get; set; }

        public bool Default { get; set; }

        public string? Value { get; set; }

        public Player? Player { get; set; }

        public override string? ToString() => Value;

        public override bool IsSimilar(object? obj) => obj is Phone other && Value == other.Value && Player == other.Player;

        public override void SetFrom(object? from)
        {
            if (from is Phone phone)
            {
                Value = phone.Value;
                Label = phone.Label;
                Default = phone.Default;
            }
        }
    }
}
