// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Application.Dtos;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Application.Dtos
{
    public class TeamDto : EntityDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public byte[]? Logo { get; set; }

        public Country? Country { get; set; }

        public string? AwayColor { get; set; }

        public string? HomeColor { get; set; }

        public StadiumDto? Stadium { get; set; }

        public List<PlayerDto>? Players { get; set; }

        public List<ManagerDto>? Staff { get; set; }
    }
}
