// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Teamup.Plugins.Contracts.Dtos
{
    public class RatedPositionImportDto
    {
        public string? Position { get; set; }

        public string? Rating { get; set; }

        public bool IsNatural { get; set; }
    }
}
