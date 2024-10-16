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
    public abstract class RoundsBuilder : IRoundsBuilder
    {
        private readonly IScheduler<IMatchesStage> _scheduler;
        private readonly IMatchesScheduler? _venuesScheduler;

        public RoundsBuilder(IScheduler<IMatchesStage> scheduler, IMatchesScheduler? venuesScheduler = null)
        {
            _scheduler = scheduler;
            _venuesScheduler = venuesScheduler;
        }

        public string NamePattern { get; set; } = MyClubResources.RoundNamePattern;

        public string ShortNamePattern { get; set; } = MyClubResources.RoundShortNamePattern;

        public bool UsePredefinedNames { get; set; } = true;

        public bool ScheduleVenuesBeforeDates { get; set; } = false;

        public virtual IEnumerable<IRound> Build(Knockout stage, IRoundsAlgorithm algorithm)
        {
            // Create rounds and fixtures
            var rounds = BuildRounds(stage, algorithm.Compute(stage.Teams).ToList()).ToList();

            // Schedule stages
            ScheduleRounds(rounds, _scheduler, _venuesScheduler);

            // Update names
            RenameRounds(rounds);

            return rounds.OfType<IRound>();
        }

        protected virtual void ScheduleRounds(ICollection<IRound> rounds, IScheduler<IMatchesStage> scheduler, IMatchesScheduler? venuesScheduler = null)
        {
            void scheduleVenues() => venuesScheduler?.Schedule(rounds.SelectMany(x => x.GetAllMatches()).ToList());
            ScheduleVenuesBeforeDates.IfTrue(scheduleVenues);
            scheduler.Schedule(ProvideMatchesStages(rounds).ToList());
            ScheduleVenuesBeforeDates.IfFalse(scheduleVenues);
        }

        protected virtual IEnumerable<IRound> BuildRounds(Knockout stage, ICollection<BracketRound> computedRounds)
        {
            var fixturesAssociations = new Dictionary<BracketFixture, IFixture>();

            return computedRounds.Select((x, y) =>
                                  {
                                      var teams = x.Teams.Select(convertToTeam).ToList();
                                      var fixtures = x.Fixtures.ToList();
                                      var round = ComputeRound(stage, teams, fixtures, y, computedRounds.Count);
                                      fixtures.ForEach(x => fixturesAssociations.Add(x, AddFixture(round, convertToTeam(x.Team1), convertToTeam(x.Team2))));

                                      return round;
                                  });

            IVirtualTeam convertToTeam(BracketTeam team)
                => team.Type switch
                {
                    BracketTeamType.Team => team.Team!,
                    BracketTeamType.Winner => fixturesAssociations[team.Fixture!].GetWinnerTeam(),
                    BracketTeamType.Looser => fixturesAssociations[team.Fixture!].GetLooserTeam(),
                    _ => throw new NotImplementedException(),
                };
        }

        protected virtual IRound ComputeRound(Knockout stage, ICollection<IVirtualTeam> teams, ICollection<BracketFixture> fixtures, int index, int numberOfRounds)
        {
            var round = CreateRound(stage, teams, fixtures, index, numberOfRounds);
            teams.ForEach(x => round.AddTeam(x));

            return round;
        }

        protected abstract IRound CreateRound(Knockout stage, ICollection<IVirtualTeam> teams, ICollection<BracketFixture> fixtures, int index, int numberOfRounds);

        protected virtual IFixture AddFixture(IRound round, IVirtualTeam team1, IVirtualTeam team2) => round switch
        {
            RoundOfFixtures roundOfFixtures => roundOfFixtures.AddFixture(team1, team2),
            RoundOfMatches roundOfMatches => roundOfMatches.AddMatch(team1, team2),
            _ => throw new NotImplementedException(),
        };

        protected virtual DateTime ProvideDate(IRound round) => round switch
        {
            RoundOfFixtures roundOfFixtures => roundOfFixtures.Stages.MinOrDefault(x => x.Date),
            RoundOfMatches roundOfMatches => roundOfMatches.Date,
            _ => throw new NotImplementedException(),
        };

        protected virtual IEnumerable<IMatchesStage> ProvideMatchesStages(ICollection<IRound> rounds) => rounds.SelectMany(x => x switch
        {
            RoundOfFixtures roundOfFixtures => roundOfFixtures.Stages.OfType<IMatchesStage>(),
            RoundOfMatches roundOfMatches => [roundOfMatches],
            _ => throw new NotImplementedException(),
        });

        protected virtual void RenameRounds(ICollection<IRound> rounds) => rounds.ForEach((x, y) =>
        {
            var roundNumber = y + 1;
            var roundInverseNumber = rounds.Count - roundNumber + 1;
            var name = MyClubResources.ResourceManager.GetString($"Round{roundInverseNumber}");
            var shortName = MyClubResources.ResourceManager.GetString($"Round{roundInverseNumber}Abbr");
            var date = ProvideDate(x);

            x.Name = UsePredefinedNames && !string.IsNullOrEmpty(name) ? name : StageNamesFactory.ComputePattern(NamePattern, y + 1, date);
            x.ShortName = UsePredefinedNames && !string.IsNullOrEmpty(shortName) ? shortName : StageNamesFactory.ComputePattern(ShortNamePattern, y + 1, ProvideDate(x));
        });
    }
}

