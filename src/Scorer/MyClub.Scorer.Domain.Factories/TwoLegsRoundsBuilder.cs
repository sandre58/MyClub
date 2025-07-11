// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.BracketComputing;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Domain.Factories
{
    public class TwoLegsRoundsBuilder : RoundsBuilder
    {
        public bool OneLegForFirstRound { get; set; } = false;

        public bool OneLegForLastRound { get; set; } = true;

        public bool UseAwayGoals { get; set; } = false;

        public TwoLegsRoundsBuilder(IScheduler<IMatchesStage> scheduler, IVenueScheduler? venuesScheduler = null) : base(scheduler, venuesScheduler)
        {
        }

        protected override Round CreateRound(Knockout stage, Round? ancestor, BracketType bracketType, int index, bool isLastRound)
        {
            var format = (index == 0 && OneLegForFirstRound) || (isLastRound && OneLegForLastRound)
                ? (IRoundFormat)new OneLegFormat(stage.MatchFormat.RegulationTime, stage.MatchFormat.ExtraTime, stage.MatchFormat.NumberOfPenaltyShootouts ?? 5)
                : new TwoLegsFormat(stage.MatchFormat.RegulationTime, stage.MatchFormat.ExtraTime, UseAwayGoals, stage.MatchFormat.NumberOfPenaltyShootouts ?? 5);

            return ancestor is not null ? new Round(ancestor, format, [DateTime.Today], bracketType == BracketType.Winner, $" ", $" ") : new Round(stage, format, [DateTime.Today], $" ", $" ");
        }
    }
}

