// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.BracketComputing;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Factories
{
    public class RoundsWithReplayBuilder : RoundsBuilder
    {
        public string MainStageName { get; set; } = MyClubResources.MainStage;

        public string MainStageShortName { get; set; } = MyClubResources.MainStageAbbr;

        public string ReplayStageName { get; set; } = MyClubResources.ReplayStage;

        public string ReplayStageShortName { get; set; } = MyClubResources.ReplayStageAbbr;

        public RoundsWithReplayBuilder(IScheduler<IMatchesStage> scheduler, IMatchesScheduler? venuesScheduler = null) : base(scheduler, venuesScheduler)
        {
        }

        protected override IRound CreateRound(Knockout stage, ICollection<IVirtualTeam> teams, ICollection<BracketFixture> fixtures, int index, int numberOfRounds)
        {
            var round = new RoundOfFixtures(stage, $" ");
            round.AddStage(DateTime.Today, MainStageName, MainStageShortName);
            round.AddStage(DateTime.Today, ReplayStageName, ReplayStageShortName);

            return round;
        }

        protected override IFixture AddFixture(IRound round, IVirtualTeam team1, IVirtualTeam team2)
        {
            var fixture = (Fixture)base.AddFixture(round, team1, team2);
            var roundOfFixtures = (RoundOfFixtures)round;

            if (roundOfFixtures.Stages.GetByIndex(0) is RoundStage mainStage)
                mainStage.AddMatch(fixture);

            return fixture;
        }
    }
}

