// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Application.Dtos;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Dtos
{
    public class TeamDto : EntityDto
    {
        public Guid? TemporaryId { get; set; }

        public ClubDto? Club { get; set; }

        public Category? Category { get; set; }

        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public string? AwayColor { get; set; }

        public string? HomeColor { get; set; }

        public int Order { get; set; }

        public StadiumDto? Stadium { get; set; }
    }
}
