// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Application.Dtos;
using MyClub.Domain.Enums;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Application.Dtos
{
    public class StadiumDto : EntityDto
    {
        public string? Name { get; set; }

        public Ground Ground { get; set; }

        public Address? Address { get; set; }
    }
}
