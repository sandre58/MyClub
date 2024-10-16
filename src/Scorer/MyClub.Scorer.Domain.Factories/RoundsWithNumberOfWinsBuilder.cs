// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.BracketComputing;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Factories
{
    public class RoundsWithNumberOfWinsBuilder : RoundsBuilder
    {
        public int NumberOfWins { get; set; } = 2;

        public string RoundStageNamePattern { get; set; } = MyClubResources.RoundStageNamePattern;

        public string RoundStageShortNamePattern { get; set; } = MyClubResources.RoundStageShortNamePattern;

        public bool[] InvertTeamsByStage { get; set; } = [false, true];

        public int GetNumberOfWinsMaximum() => NumberOfWins * 2 - 1;

        public RoundsWithNumberOfWinsBuilder(IScheduler<IMatchesStage> scheduler, IMatchesScheduler? venuesScheduler = null) : base(scheduler, venuesScheduler)
        {
        }

        protected override IRound CreateRound(Knockout stage, ICollection<IVirtualTeam> teams, ICollection<BracketFixture> fixtures, int index, int numberOfRounds)
        {
            var round = new RoundOfFixtures(stage, $" ");

            for (var i = 1; i <= GetNumberOfWinsMaximum(); i++)
                round.AddStage(DateTime.Today, i.ToString(MyClubResources.MatchX), i.ToString(MyClubResources.MatchXAbbr));
            return round;
        }

        protected override IFixture AddFixture(IRound round, IVirtualTeam team1, IVirtualTeam team2)
        {
            var fixture = (Fixture)base.AddFixture(round, team1, team2);
            var roundOfFixtures = (RoundOfFixtures)round;

            for (var i = 0; i < NumberOfWins; i++)
            {
                var invertTeams = InvertTeamsByStage.ElementAtOrDefault(i);
                roundOfFixtures.Stages.GetByIndex(i)?.AddMatch(fixture, invertTeams);
            }

            return fixture;
        }

        protected override void RenameRounds(ICollection<IRound> rounds)
        {
            base.RenameRounds(rounds);

            foreach (var round in rounds.OfType<RoundOfFixtures>())
            {
                round.Stages.OrderBy(x => x.Date).ForEach((x, y) =>
                {
                    x.Name = StageNamesFactory.ComputePattern(RoundStageNamePattern, y + 1, x.Date);
                    x.ShortName = StageNamesFactory.ComputePattern(RoundStageShortNamePattern, y + 1, x.Date);
                });
            }
        }
    }
}
