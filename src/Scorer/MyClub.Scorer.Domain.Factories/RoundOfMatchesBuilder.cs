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
    public class RoundOfMatchesBuilder : IKnockoutBuilder
    {
        private readonly IScheduler<RoundOfMatches> _scheduler;
        private readonly IMatchesScheduler? _venuesScheduler;

        public RoundOfMatchesBuilder(IScheduler<RoundOfMatches> scheduler, IMatchesScheduler? venuesScheduler = null)
        {
            _scheduler = scheduler;
            _venuesScheduler = venuesScheduler;
        }

        public string NamePattern { get; set; } = "{round}";

        public string ShortNamePattern { get; set; } = "{roundAbbr}";

        public bool ScheduleVenuesBeforeDates { get; set; } = false;

        public virtual IEnumerable<IRound> Build<T>(T stage, IRoundsAlgorithm algorithm) where T : Knockout, ITeamsProvider
        {
            var fixturesAssociations = new Dictionary<BracketFixture, IFixture>();

            // 1 - Create rounds and fixtures
            var rounds = algorithm.Compute(stage.ProvideTeams())
                                  .Select((x, y) =>
                                  {
                                      var round = new RoundOfMatches(stage, DateTime.Today, $"{MyClubResources.Round} {y + 1}", (y + 1).ToString(MyClubResources.RoundXAbbr));
                                      x.Teams.ForEach(x => round.AddTeam(convertToTeam(x)));
                                      x.Fixtures.ForEach(x => fixturesAssociations.Add(x, round.AddMatch(convertToTeam(x.Team1), convertToTeam(x.Team2))));

                                      return round;
                                  })
                                  .ToList();

            // Schedule stages
            void scheduleVenues() => _venuesScheduler?.Schedule(rounds.SelectMany(x => x.GetAllMatches()).ToList());
            ScheduleVenuesBeforeDates.IfTrue(scheduleVenues);
            _scheduler.Schedule(rounds);
            ScheduleVenuesBeforeDates.IfFalse(scheduleVenues);

            // Update names
            rounds.OrderBy(x => x.Date).ForEach((x, y) =>
            {
                x.Name = StageNamesFactory.ComputePattern(NamePattern, y + 1, x);
                x.ShortName = StageNamesFactory.ComputePattern(ShortNamePattern, y + 1, x);
            });

            return rounds;

            IVirtualTeam convertToTeam(BracketTeam team)
                => team.Type switch
                {
                    BracketTeamType.Team => team.Team!,
                    BracketTeamType.Winner => fixturesAssociations[team.Fixture!].GetWinnerTeam(),
                    BracketTeamType.Looser => fixturesAssociations[team.Fixture!].GetLooserTeam(),
                    _ => throw new NotImplementedException(),
                };
        }
    }
}

