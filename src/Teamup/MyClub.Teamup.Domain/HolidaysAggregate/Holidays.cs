// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Extensions;

namespace MyClub.Teamup.Domain.HolidaysAggregate
{
    public class Holidays : AuditableEntity, IAggregateRoot, IHasPeriod
    {
        private string _label = string.Empty;

        public Holidays(DateTime startDate, DateTime endDate, string label, Guid? id = null) : base(id)
            => (Period, Label) = (new(startDate, endDate), label);

        public string Label
        {
            get => _label;
            set => _label = value.IsRequiredOrThrow();
        }

        public ObservablePeriod Period { get; }

        public override string? ToString() => Label;
    }
}
