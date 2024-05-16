// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Dtos
{
    public class SquadPlayerExportDto : SquadPlayerDto
    {
        public int? Age { get; set; }
        public string? Team { get; set; }
        public Position? Position { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Street { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
    }
}
