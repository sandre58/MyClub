// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.BracketComputing;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.Factories
{
    public class RoundsWithMatchesBuilder : RoundsBuilder
    {
        public RoundsWithMatchesBuilder(IScheduler<IMatchesStage> scheduler, IMatchesScheduler? venuesScheduler = null) : base(scheduler, venuesScheduler)
        {
        }

        protected override IRound CreateRound(Knockout stage, ICollection<IVirtualTeam> teams, ICollection<BracketFixture> fixtures, int index, int numberOfRounds) => new RoundOfMatches(stage, DateTime.Today, $" ");
    }
}

