// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Scorer.Plugins.Contracts.Dtos
{
    public class TeamImportDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public byte[]? Logo { get; set; }

        public string? Country { get; set; }

        public string? AwayColor { get; set; }

        public string? HomeColor { get; set; }

        public StadiumImportDto? Stadium { get; set; }
    }
}
