// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Application.Dtos;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Dtos
{
    public class MatchDto : EntityDto
    {
        public Guid? HomeTeamId { get; set; }

        public Guid? AwayTeamId { get; set; }

        public DateTime Date { get; set; }

        public MatchFormat? Format { get; set; }

        public StadiumDto? Stadium { get; set; }

        public bool NeutralVenue { get; set; }

        public bool AfterExtraTime { get; set; }

        public int? HomeScore { get; set; }

        public int? AwayScore { get; set; }

        public int? HomeShootoutScore { get; set; }

        public int? AwayShootoutScore { get; set; }

        public bool HomeIsWithdrawn { get; set; }

        public bool AwayIsWithdrawn { get; set; }

        public int HomePenaltyPoints { get; set; }

        public int AwayPenaltyPoints { get; set; }

        public MatchState State { get; set; }

        public DateTime? PostponedDate { get; set; }

        public Guid? ParentId { get; set; }
    }
}
