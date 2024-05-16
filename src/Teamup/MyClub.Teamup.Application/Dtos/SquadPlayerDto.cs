// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Teamup.Domain.Enums;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Dtos
{
    public class SquadPlayerDto : PlayerDto
    {
        public Guid PlayerId { get; set; }

        public Guid? TeamId { get; set; }

        public Category? Category { get; set; }

        public LicenseState LicenseState { get; set; }

        public bool IsMutation { get; set; }

        public List<AbsenceDto>? Absences { get; set; }

        public int? Number { get; set; }

        public int? ShoesSize { get; set; }
    }
}
