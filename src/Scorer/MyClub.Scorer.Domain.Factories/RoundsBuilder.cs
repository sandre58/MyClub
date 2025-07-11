// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.BracketComputing;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Humanizer;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Factories
{
    public readonly struct AddConsolationParameters
    {
        public static readonly AddConsolationParameters None = new(null, null, -1);

        public static readonly AddConsolationParameters All = From(1, int.MaxValue);

        public static readonly AddConsolationParameters OneConsolationBracket = For(1, 1);

        public static AddConsolationParameters ThirdPlace(int numberOfRounds) => For(numberOfRounds - 1, 1);

        public static AddConsolationParameters From(int fromRound, int maximumLevel = int.MaxValue) => new(null, fromRound, maximumLevel);

        public static AddConsolationParameters For(int forRound, int maximumLevel = int.MaxValue) => new(forRound, null, maximumLevel);

        private AddConsolationParameters(int? forRound, int? fromRound, int maximumLevel)
        {
            ForRound = forRound;
            FromRound = fromRound;
            MaximumLevel = maximumLevel;
        }

        public int? ForRound { get; }

        public int? FromRound { get; }

        public int MaximumLevel { get; }
    }

    public abstract class RoundsBuilder : IRoundsBuilder
    {
        private readonly IScheduler<IMatchesStage> _scheduler;
        private readonly IVenueScheduler? _venuesScheduler;

        public RoundsBuilder(IScheduler<IMatchesStage> scheduler, IVenueScheduler? venuesScheduler = null)
        {
            _scheduler = scheduler;
            _venuesScheduler = venuesScheduler;
        }

        public string NamePattern { get; set; } = MyClubResources.RoundNamePattern;

        public string ShortNamePattern { get; set; } = MyClubResources.RoundShortNamePattern;

        public bool UsePredefinedNames { get; set; } = true;

        public bool ScheduleVenuesBeforeDates { get; set; } = false;

        public AddConsolationParameters? AddConsolationParameters { get; set; }

        public virtual IEnumerable<Round> Build(Knockout stage, IRoundsAlgorithm algorithm)
        {
            // Create rounds and fixtures
            var fixturesAssociations = new Dictionary<BracketFixture, IFixture>();
            var rounds = BuildRounds(stage, null, stage.Teams, algorithm.Compute(stage.Teams), fixturesAssociations, BracketType.Winner, -1);

            var orderedRounds = rounds.OrderBy(x => x.index).Select(x => (x.index, x.round)).ToList();

            // Schedule stages
            ScheduleRounds(orderedRounds.Select(x => x.round).ToList(), _scheduler, _venuesScheduler);

            // Update names
            RenameRounds(orderedRounds);

            return orderedRounds.Select(x => x.round);
        }

        protected virtual void ScheduleRounds(ICollection<Round> rounds, IScheduler<IMatchesStage> scheduler, IVenueScheduler? venuesScheduler = null)
        {
            void scheduleVenues() => venuesScheduler?.Schedule(rounds.SelectMany(x => x.GetAllMatches()).ToList());
            ScheduleVenuesBeforeDates.IfTrue(scheduleVenues);
            scheduler.Schedule(rounds.SelectMany(x => x.Stages).ToList());
            ScheduleVenuesBeforeDates.IfFalse(scheduleVenues);
        }

        private List<(int index, Round round)> BuildRounds(Knockout stage,
                                                           Round? ancestor,
                                                           IEnumerable<IVirtualTeam> teams,
                                                           BracketRound tree,
                                                           Dictionary<BracketFixture, IFixture> fixturesAssociations,
                                                           BracketType bracketType,
                                                           int previousIndex)
        {
            var currentIndex = previousIndex + 1;
            var rounds = new List<(int index, Round round)>();
            var winnerChildren = tree.Children.GetOrDefault(BracketType.Winner);
            var looserChildren = tree.Children.GetOrDefault(BracketType.Looser);

            var currentRound = BuildRound(stage, ancestor, teams, fixturesAssociations, tree.Fixtures, bracketType, currentIndex, !tree.Children.Any());
            rounds.Add((currentIndex, currentRound));

            var addLooserRound = AddConsolationParameters.HasValue
                                && ((AddConsolationParameters.Value.ForRound.HasValue && currentIndex == AddConsolationParameters.Value.ForRound - 1)
                                    || (AddConsolationParameters.Value.FromRound.HasValue && currentIndex >= AddConsolationParameters.Value.FromRound - 1))
                                && currentRound.GetConsolationLevel() < AddConsolationParameters.Value.MaximumLevel;

            if (addLooserRound && looserChildren is not null)
                rounds.AddRange(BuildRounds(stage, currentRound, [], looserChildren, fixturesAssociations, BracketType.Looser, currentIndex));

            if (winnerChildren is not null)
                rounds.AddRange(BuildRounds(stage, currentRound, [], winnerChildren, fixturesAssociations, BracketType.Winner, currentIndex));

            return rounds;
        }

        private Round BuildRound(Knockout stage,
                                 Round? ancestor,
                                 IEnumerable<IVirtualTeam> teams,
                                 Dictionary<BracketFixture, IFixture> fixturesAssociations,
                                 ICollection<BracketFixture> fixtures,
                                 BracketType bracketType,
                                 int index,
                                 bool isLastRound)
        {
            var round = CreateRound(stage, ancestor, bracketType, index, isLastRound);
            teams.ForEach(x => round.AddTeam(x));
            var rank = isLastRound ? round.GetConsolationLevel() * 2 + 1 : (int?)null;
            fixtures.ForEach(x => fixturesAssociations.Add(x, AddFixture(round, convertToTeam(x.Team1), convertToTeam(x.Team2), rank)));
            var remainingTeams = round.Fixtures.SelectMany(x => new[] { x.Team1, x.Team2 }).Except(round.ProvideTeams()).ToList();
            remainingTeams.ForEach(x => round.AddTeam(x));

            IVirtualTeam convertToTeam(BracketTeam team)
            => team.Type switch
            {
                BracketTeamType.Team => team.Team!,
                BracketTeamType.Winner => fixturesAssociations[team.Fixture!].GetWinnerTeam(),
                BracketTeamType.Looser => fixturesAssociations[team.Fixture!].GetLooserTeam(),
                _ => throw new NotImplementedException(),
            };

            return round;
        }

        protected abstract Round CreateRound(Knockout stage, Round? ancestor, BracketType bracketType, int index, bool isLastRound);

        protected virtual IFixture AddFixture(Round round, IVirtualTeam team1, IVirtualTeam team2, int? rank = null) => round.AddFixture(team1, team2, rank);

        protected virtual DateTime ProvideDate(Round round) => round.Stages.MinOrDefault(x => x.Date);

        protected virtual void RenameRounds(ICollection<(int index, Round round)> rounds) => rounds.ForEach(x =>
        {
            var round = x.round;
            var roundNumber = x.index + 1;
            var roundInverseNumber = rounds.MaxOrDefault(y => y.index) - x.index + 1;
            var date = ProvideDate(x.round);

            if (!UsePredefinedNames)
            {
                x.round.Name = StageNamesFactory.ComputePattern(NamePattern, roundNumber, date);
                x.round.ShortName = StageNamesFactory.ComputePattern(ShortNamePattern, roundNumber, date);
            }
            else
            {
                var matchesWithRank = round.Fixtures.Where(x => x.Rank.HasValue).ToList();

                if (matchesWithRank.Count > 1)
                {
                    x.round.Name = MyClubResources.RankingMatches;
                    x.round.ShortName = MyClubResources.RankingMatchesAbbr;
                }
                else if (matchesWithRank.Count == 1 && matchesWithRank.First().Rank > 1)
                {
                    x.round.Name = MyClubResources.MatchForXPlace.FormatWith(matchesWithRank.First().Rank!.Value.Ordinalize());
                    x.round.ShortName = MyClubResources.MatchForXPlaceAbbr.FormatWith(matchesWithRank.First().Rank!.Value.Ordinalize());
                }
                else
                {
                    var consolationLevel = round.GetConsolationLevel();
                    var name = MyClubResources.ResourceManager.GetString($"Round{roundInverseNumber}");
                    var shortName = MyClubResources.ResourceManager.GetString($"Round{roundInverseNumber}Abbr");

                    var consolationSuffix = consolationLevel switch
                    {
                        < 1 => string.Empty,
                        < 2 => $" - {MyClubResources.Consolation}",
                        _ => $" - {MyClubResources.Consolation} ({MyClubResources.LevelXAbbr.FormatWith(consolationLevel)})",
                    };

                    var consolationPrefix = consolationLevel switch
                    {
                        < 1 => string.Empty,
                        < 2 => $"{MyClubResources.ConsolationAbbr}",
                        _ => $"{MyClubResources.ConsolationAbbr}{consolationLevel}",
                    };

                    x.round.Name = (!string.IsNullOrEmpty(name) ? name : StageNamesFactory.ComputePattern(NamePattern, roundNumber, date)) + consolationSuffix;
                    x.round.ShortName = consolationPrefix + (!string.IsNullOrEmpty(shortName) ? shortName : StageNamesFactory.ComputePattern(ShortNamePattern, roundNumber, date));
                }
            }
        });
    }
}

