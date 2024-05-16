// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Sequences;

namespace MyClub.Domain.Training.TrainingAggregate.DrawingObjects
{
    public class DrawingObject : Entity
    {
        public static readonly AcceptableValueRange<double> AcceptableRangePosition = new(0, 100);
        public static readonly AcceptableValueRange<double> AcceptableRangeRotation = new(0, 360);

        private string _name = string.Empty;
        private double _x;
        private double _y;
        private double _rotation;

        protected DrawingObject(Guid? id = null) : base(id) { }

        public virtual string Name
        {
            get => _name;
            set => _name = value.IsRequiredOrThrow();
        }

        public double X
        {
            get => _x;
            set => _x = AcceptableRangePosition.ValidateOrThrow(value);
        }

        public double Y
        {
            get => _y;
            set => _y = AcceptableRangePosition.ValidateOrThrow(value);
        }

        public double Rotation
        {
            get => _rotation;
            set => _rotation = AcceptableRangePosition.ValidateOrThrow(value);
        }

        public override string? ToString() => $"{Name} - X={X} - Y={Y} - Rotation={Rotation}°";
    }
}
