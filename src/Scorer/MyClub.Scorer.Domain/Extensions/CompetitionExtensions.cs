// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Extensions
{
    public static class CompetitionExtensions
    {
        public static T? GetStage<T>(this ICompetition competition, Guid id) where T : ICompetitionStage => competition.GetStages<T>().GetByIdOrDefault(id);

        public static T? GetStage<T>(this ICompetition competition, Guid? id) where T : IStage => id.HasValue ? (T?)competition.GetStages<IStage>().GetByIdOrDefault(id.Value) : (T)competition;

        public static T? GetStage<T>(this IStage stage, Guid id) where T : ICompetitionStage => stage.GetStages<T>().GetByIdOrDefault(id);

        public static IEnumerable<ISchedulable> GetAllSchedulables(this ICompetition competition) => competition.GetAllMatches().OfType<ISchedulable>().Union(competition.GetStages<IMatchesStage>());

        public static IEnumerable<ISchedulable> GetAllSchedulables(this IStage stage) => stage.GetAllMatches().OfType<ISchedulable>().Union(stage.GetStages<IMatchesStage>());
    }
}
