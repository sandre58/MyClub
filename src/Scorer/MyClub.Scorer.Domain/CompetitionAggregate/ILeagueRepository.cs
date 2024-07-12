﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Domain.ProjectAggregate
{

    public interface ILeagueRepository
    {
        bool HasCurrent();

        League GetCurrentOrThrow();

        void UpdateRankingRules(RankingRules rules);

        void UpdateLabels(IDictionary<AcceptableValueRange<int>, RankLabel> labels);

        void UpdatePenaltyPoints(Dictionary<Guid, int> penaltyPoints);

        void UpdateMatchFormat(MatchFormat format);

        void UpdateSchedulingParameters(SchedulingParameters schedulingParameters);
    }
}
