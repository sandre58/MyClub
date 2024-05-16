// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.DatabaseContext.Domain.PlayerAggregate
{
    public class Injury : Entity
    {
        public Injury() { }

        public Injury(Guid id) : base(id) { }

        public string? Condition { get; set; }

        public string? Severity { get; set; }

        public string? Type { get; set; }

        public string? Category { get; set; }

        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public Player? Player { get; set; }

        public override string? ToString() => Condition;

        public override bool IsSimilar(object? obj) => obj is Injury other && Condition == other.Condition && Player == other.Player && StartDate == other.StartDate;

        public override void SetFrom(object? from)
        {
            if (from is Injury injury)
            {
                Condition = injury.Condition;
                Severity = injury.Severity;
                Type = injury.Type;
                Category = injury.Category;
                Description = injury.Description;
                StartDate = injury.StartDate;
                EndDate = injury.EndDate;
            }
        }
    }
}
