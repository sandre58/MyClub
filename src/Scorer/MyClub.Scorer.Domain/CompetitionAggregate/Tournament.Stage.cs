﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using MyClub.Domain;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface IStage : IEntity, ISchedulingParametersProvider
    {
        string Name { get; }

        IStage? Parent { get; }

        ReadOnlyObservableCollection<ITeam> Teams { get; }

        IEnumerable<Match> GetAllMatches();
    }
}
