// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Dtos
{
    public class PlayerDto : PersonDto
    {
        public Laterality Laterality { get; set; }

        public int? Height { get; set; }

        public int? Weight { get; set; }

        public List<RatedPositionDto>? Positions { get; set; }

        public List<InjuryDto>? Injuries { get; set; }
    }
}
