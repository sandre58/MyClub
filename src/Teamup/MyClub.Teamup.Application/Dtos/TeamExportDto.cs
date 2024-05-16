// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Application.Dtos
{
    public class TeamExportDto
    {
        public byte[]? Logo { get; set; }

        public string? Club { get; set; }

        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public Category? Category { get; set; }

        public Country? Country { get; set; }

        public StadiumDto? Stadium { get; set; }

        public string? AwayColor { get; set; }

        public string? HomeColor { get; set; }
    }
}
