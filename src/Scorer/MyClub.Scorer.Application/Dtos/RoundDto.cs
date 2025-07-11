// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Application.Dtos
{
    public class RoundDto : EntityDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public MatchRules? MatchRules { get; set; }

        public SchedulingParameters? SchedulingParameters { get; set; }

        public Guid? StageId { get; set; }

        public List<RoundStageDto>? Stages { get; set; }
    }
}
