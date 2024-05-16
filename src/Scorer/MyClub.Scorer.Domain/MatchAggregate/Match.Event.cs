// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public abstract class MatchEvent : Entity
    {
        protected MatchEvent(int? minute = null, Guid? id = null) : base(id) => Minute = minute;

        public int? Minute { get; set; }
    }
}
