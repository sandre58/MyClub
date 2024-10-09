// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Application.Dtos
{
    public class MatchDto : EntityDto
    {
        public Guid HomeTeamId { get; set; }

        public Guid AwayTeamId { get; set; }

        public DateTime Date { get; set; }

        public StadiumDto? Stadium { get; set; }

        public bool IsNeutralStadium { get; set; }

        public bool AfterExtraTime { get; set; }

        public int? HomeScore { get; set; }

        public int? AwayScore { get; set; }

        public List<GoalDto>? HomeGoals { get; set; }

        public List<GoalDto>? AwayGoals { get; set; }

        public List<PenaltyShootoutDto>? HomeShootout { get; set; }

        public List<PenaltyShootoutDto>? AwayShootout { get; set; }

        public List<CardDto>? HomeCards { get; set; }

        public List<CardDto>? AwayCards { get; set; }

        public int? HomeShootoutScore { get; set; }

        public int? AwayShootoutScore { get; set; }

        public bool HomeIsWithdrawn { get; set; }

        public bool AwayIsWithdrawn { get; set; }

        public MatchState State { get; set; }

        public DateTime? PostponedDate { get; set; }

        public Guid StageId { get; set; }

        public MatchFormat? Format { get; set; }

        public MatchRules? Rules { get; set; }

        public bool ScheduleAutomatic { get; set; }

        public bool ScheduleStadiumAutomatic { get; set; }
    }
}
