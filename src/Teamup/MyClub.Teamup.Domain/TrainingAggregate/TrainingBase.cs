// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyClub.Domain;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Sequences;

namespace MyClub.Teamup.Domain.TrainingAggregate
{
    public class TrainingBase : AuditableEntity, IAggregateRoot
    {
        public static readonly AcceptableValueRange<double> AcceptableRangeRating = new(0, 5);

        private string _theme = string.Empty;
        private double? _technicalRating;
        private double? _tacticalRating;
        private double? _physicalRating;
        private double? _mentalRating;

        protected TrainingBase(string theme, Guid? id = null) : base(id) => Theme = theme;

        public virtual string Theme
        {
            get => _theme;
            set => _theme = value.IsRequiredOrThrow();
        }

        public ObservableCollection<string> Stages { get; } = [];

        public ObservableCollection<string> TechnicalGoals { get; } = [];

        public ObservableCollection<string> TacticalGoals { get; } = [];

        public ObservableCollection<string> PhysicalGoals { get; } = [];

        public ObservableCollection<string> MentalGoals { get; } = [];

        public double? TechnicalRating
        {
            get => _technicalRating;
            set => _technicalRating = AcceptableRangeRating.ValidateOrThrow(value);
        }

        public double? TacticalRating
        {
            get => _tacticalRating;
            set => _tacticalRating = AcceptableRangeRating.ValidateOrThrow(value);
        }

        public double? PhysicalRating
        {
            get => _physicalRating;
            set => _physicalRating = AcceptableRangeRating.ValidateOrThrow(value);
        }

        public double? MentalRating
        {
            get => _mentalRating;
            set => _mentalRating = AcceptableRangeRating.ValidateOrThrow(value);
        }

        public string? Notes { get; }

        public override string? ToString() => Theme;
    }
}
