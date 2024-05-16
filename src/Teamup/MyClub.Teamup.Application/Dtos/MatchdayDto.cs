// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Application.Dtos;

namespace MyClub.Teamup.Application.Dtos
{
    public class MatchdayDto : EntityDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public DateTime Date { get; set; }

        public bool InvertTeams { get; set; }

        public Guid? DuplicatedMatchdayId { get; set; }

        public bool IsPostponed { get; set; }

        public DateTime? PostponedDate { get; set; }

        public Guid? ParentId { get; set; }
    }
}
