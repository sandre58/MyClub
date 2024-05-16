// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;

namespace MyClub.Scorer.Application.Dtos
{
    public class MatchdayDto : EntityDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public DateTime Date { get; set; }

        public bool IsPostponed { get; set; }

        public DateTime? PostponedDate { get; set; }

        public Guid? ParentId { get; set; }

        public List<Guid>? MatchesToDelete { get; set; }

        public List<MatchDto>? MatchesToAdd { get; set; }
    }
}
