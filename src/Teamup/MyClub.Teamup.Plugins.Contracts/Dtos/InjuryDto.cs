// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Teamup.Plugins.Contracts.Dtos
{
    public class InjuryDto
    {
        public string? Condition { get; set; }

        public string? Severity { get; set; }

        public string? Type { get; set; }

        public string? Category { get; set; }

        public string? Description { get; set; }

        public DateTime Date { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
