// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;

namespace MyClub.Scorer.Application.Dtos
{
    public class GoalDto : MatchEventDto
    {
        public GoalType Type { get; set; }

        public Guid? ScorerId { get; set; }

        public Guid? AssistId { get; set; }
    }
}
