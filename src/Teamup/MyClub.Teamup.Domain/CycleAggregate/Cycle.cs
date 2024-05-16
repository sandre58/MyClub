// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Domain;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Extensions;

namespace MyClub.Teamup.Domain.CycleAggregate
{
    public class Cycle : AuditableEntity, IAggregateRoot, IHasPeriod
    {
        private string _label = string.Empty;

        public Cycle(DateTime startDate, DateTime endDate, string label, Guid? id = null) : base(id)
            => (Period, Label) = (new(startDate, endDate), label);

        public string Label
        {
            get => _label;
            set => _label = value.IsRequiredOrThrow();
        }

        public ObservablePeriod Period { get; }

        public string? Color { get; set; }

        public ICollection<string> TechnicalGoals { get; } = [];

        public ICollection<string> TacticalGoals { get; } = [];

        public ICollection<string> PhysicalGoals { get; } = [];

        public ICollection<string> MentalGoals { get; } = [];

        public override string? ToString() => Label;
    }
}
