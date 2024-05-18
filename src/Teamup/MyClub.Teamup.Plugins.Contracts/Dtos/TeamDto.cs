// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Teamup.Plugins.Contracts.Dtos
{
    public class TeamDto
    {
        public ClubDto? Club { get; set; }

        public string? Category { get; set; }

        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public string? AwayColor { get; set; }

        public string? HomeColor { get; set; }

        public StadiumDto? Stadium { get; set; }
    }
}
