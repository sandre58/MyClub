// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.BracketComputing;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Domain.Factories
{
    public class NumberOfWinsRoundsBuilder : RoundsBuilder
    {
        public int NumberOfWins { get; set; } = 2;

        public bool[] InvertTeamsByStage { get; set; } = [false, true];

        public int GetNumberOfWinsMaximum() => NumberOfWins * 2 - 1;

        public NumberOfWinsRoundsBuilder(IScheduler<IMatchesStage> scheduler, IVenueScheduler? venuesScheduler = null) : base(scheduler, venuesScheduler)
        {
        }

        protected override Round CreateRound(Knockout stage, Round? ancestor, BracketType bracketType, int index, bool isLastRound)
        {
            var format = new NumberOfWinsFormat(NumberOfWins, InvertTeamsByStage, stage.MatchFormat.RegulationTime, stage.MatchFormat.ExtraTime, stage.MatchFormat.NumberOfPenaltyShootouts ?? 5);
            var round = ancestor is not null ? new Round(ancestor, format, [DateTime.Today], bracketType == BracketType.Winner, $" ", $" ") : new Round(stage, format, [DateTime.Today], $" ", $" ");

            for (var i = 0; i < format.GetMaximumOfStages() - format.NumberOfWins; i++)
                round.AddStage(DateTime.Today);
            return round;
        }
    }
}
