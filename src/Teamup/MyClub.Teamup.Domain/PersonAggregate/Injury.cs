// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Extensions;
using MyClub.Domain;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.PersonAggregate
{
    public class Injury : AuditableEntity
    {
        private string _condition = null!;

        public Injury(DateTime date, string condition, InjurySeverity severity, DateTime? endDate = null, InjuryType type = InjuryType.Other, InjuryCategory category = InjuryCategory.Other, Guid? id = null) : base(id)
        {
            Period = new(date, endDate);
            Condition = condition;
            Severity = severity;
            Type = type;
            Category = category;
        }

        public string Condition
        {
            get => _condition;
            set => _condition = value.IsRequiredOrThrow();
        }

        public InjurySeverity Severity { get; set; }

        public InjuryType Type { get; set; }

        public InjuryCategory Category { get; set; }

        public string? Description { get; set; }

        public ObservablePeriodWithOptionalEnd Period { get; }

        public bool IsCurrent() => Period.Contains(DateTime.Today);

        public override string ToString() => Condition;
    }
}
