// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain;
using MyClub.Teamup.Domain.Enums;
using MyNet.Utilities.Sequences;

namespace MyClub.Teamup.Domain.TrainingAggregate
{
    public class Area : AuditableEntity
    {
        public static readonly AcceptableValueRange<double> AcceptableRangeSize = new(1, 100);

        private double _width = 98;
        private double _height = 98;

        public Area(AreaType type) => Type = type;

        public AreaType Type { get; }

        public double Height
        {
            get => _height;
            set => _height = AcceptableRangeSize.ValidateOrThrow(value);
        }

        public double Width
        {
            get => _width;
            set => _width = AcceptableRangeSize.ValidateOrThrow(value);
        }

        public override string ToString() => $"{Type} - Width={_width} - Height={_height}";
    }
}
