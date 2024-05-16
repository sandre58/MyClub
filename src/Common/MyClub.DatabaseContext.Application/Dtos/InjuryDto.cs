// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Application.Dtos;
using MyClub.Domain.Enums;

namespace MyClub.DatabaseContext.Application.Dtos
{
    public class InjuryDto : EntityDto
    {
        public string? Condition { get; set; }

        public InjurySeverity Severity { get; set; }

        public InjuryType Type { get; set; }

        public InjuryCategory Category { get; set; }

        public string? Description { get; set; }

        public DateTime Date { get; set; }

        public DateTime? EndDate { get; set; }

        public Guid? PlayerId { get; set; }
    }
}
