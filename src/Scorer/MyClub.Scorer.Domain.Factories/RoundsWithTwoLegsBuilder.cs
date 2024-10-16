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
    public class RoundsWithTwoLegsBuilder : RoundsBuilder
    {
        public string FirstLegName { get; set; } = MyClubResources.FirstLeg;

        public string FirstLegShortName { get; set; } = MyClubResources.FirstLegAbbr;

        public string SecondLegName { get; set; } = MyClubResources.SecondLeg;

        public string SecondLegShortName { get; set; } = MyClubResources.SecondLegAbbr;

        public bool OneLegForFirstRound { get; set; } = false;

        public bool OneLegForLastRound { get; set; } = true;

        public RoundsWithTwoLegsBuilder(IScheduler<IMatchesStage> scheduler, IMatchesScheduler? venuesScheduler = null) : base(scheduler, venuesScheduler)
        {
        }

        protected override IRound CreateRound(Knockout stage, ICollection<IVirtualTeam> teams, ICollection<BracketFixture> fixtures, int index, int numberOfRounds)
        {
            if ((index == 0 && OneLegForFirstRound) || (index == numberOfRounds - 1 && OneLegForLastRound))
                return new RoundOfMatches(stage, DateTime.Today, $" ");
            else
            {
                var round = new RoundOfFixtures(stage, $" ");
                round.AddStage(DateTime.Today, FirstLegName, FirstLegShortName);
                round.AddStage(DateTime.Today, SecondLegName, SecondLegShortName);

                return round;
            }
        }

        protected override IFixture AddFixture(IRound round, IVirtualTeam team1, IVirtualTeam team2)
        {
            var fixture = base.AddFixture(round, team1, team2);

            if (fixture is Fixture fixtureWithMatches)
            {
                if (fixtureWithMatches.Stage.Stages.GetByIndex(0) is RoundStage firstLeg)
                    firstLeg.AddMatch(fixtureWithMatches);

                if (fixtureWithMatches.Stage.Stages.GetByIndex(1) is RoundStage secondLeg)
                    secondLeg.AddMatch(fixtureWithMatches, true);
            }

            return fixture;
        }
    }
}

