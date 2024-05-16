// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyClub.Teamup.Domain.Enums;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Extensions;

namespace MyClub.Teamup.Domain.PersonAggregate
{
    public class Absence : AuditableEntity, IHasPeriod
    {
        private string _label = string.Empty;

        public Absence(DateTime startDate, DateTime endDate, string label, Guid? id = null) : base(id)
            => (Period, Label) = (new(startDate, endDate), label);

        public AbsenceType Type { get; set; }

        public string Label
        {
            get => _label;
            set => _label = value.IsRequiredOrThrow();
        }

        public ObservablePeriod Period { get; }

        public override string? ToString() => $"{Period.Start} - {Type}";
    }
}
