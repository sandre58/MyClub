// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Sequences;

namespace MyClub.Teamup.Domain.TacticAggregate
{
    public class TacticPosition : Entity
    {
        public static readonly AcceptableValueRange<int> AcceptableRangeNumber = new(1, 99);
        public static readonly AcceptableValueRange<double> AcceptableRangeOffset = new(-100, 100);

        private int? _number;

        public TacticPosition(Position position, Guid? id = null) : base(id)
            => Position = position.IsRequiredOrThrow(nameof(Position));

        public Position Position { get; }

        public double OffsetX { get; set; }

        public double OffsetY { get; set; }

        public int? Number
        {
            get => _number;
            set => _number = AcceptableRangeNumber.ValidateOrThrow(value);
        }

        public ICollection<string> Instructions { get; } = [];

        public override string ToString() => $"{Position}";
    }
}
