// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Collections;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class Round : NameEntity, ICompetitionStage
    {
        private readonly OptimizedObservableCollection<IVirtualTeam> _teams = [];
        private readonly OptimizedObservableCollection<RoundStage> _stages = [];
        private readonly OptimizedObservableCollection<Fixture> _fixtures = [];

        public Round(Knockout stage,
                     IRoundFormat format,
                     DateTime[] dates,
                     string name,
                     string? shortName = null,
                     MatchRules? matchRules = null,
                     SchedulingParameters? schedulingParameters = null,
                     Guid? id = null) : this(stage, null, format, dates, true, name, shortName, matchRules, schedulingParameters, id) { }

        public Round(Round ancestor,
                     IRoundFormat format,
                     DateTime[] dates,
                     bool isWinnerRound,
                     string name,
                     string? shortName = null,
                     MatchRules? matchRules = null,
                     SchedulingParameters? schedulingParameters = null,
                     Guid? id = null) : this(ancestor.Stage, ancestor, format, dates, isWinnerRound, name, shortName, matchRules ?? ancestor.MatchRules.OverrideValue, schedulingParameters ?? ancestor.SchedulingParameters.OverrideValue, id) { }

        private Round(Knockout stage,
                      Round? ancestor,
                      IRoundFormat format,
                      DateTime[] dates,
                      bool isWinnerRound,
                      string name,
                      string? shortName = null,
                      MatchRules? matchRules = null,
                      SchedulingParameters? schedulingParameters = null,
                      Guid? id = null)
            : base(name, shortName, id)
        {
            IsWinnerRound = isWinnerRound;
            Ancestor = ancestor;
            Stage = stage;
            Format = format;
            MatchRules.Initialize(stage, () => stage.MatchRules);
            SchedulingParameters.Initialize(stage, () => stage.SchedulingParameters);
            matchRules.IfNotNull(MatchRules.Override);
            schedulingParameters.IfNotNull(SchedulingParameters.Override);
            Teams = new(_teams);
            Fixtures = new(_fixtures);
            Stages = new(_stages);

            Format.InitializeRound(this, dates);

            MatchRules.PropertyChanged += (sender, e) => (e.PropertyName == nameof(OverridableValue<MatchRules>.Value)).IfTrue(() => RaisePropertyChanged(nameof(MatchRules)));
            SchedulingParameters.PropertyChanged += (sender, e) => (e.PropertyName == nameof(OverridableValue<SchedulingParameters>.Value)).IfTrue(() => RaisePropertyChanged(nameof(SchedulingParameters)));
        }

        public Knockout Stage { get; }

        public Round? Ancestor { get; }

        public bool IsWinnerRound { get; }

        public IRoundFormat Format { get; set; }

        public OverridableValue<MatchRules> MatchRules { get; private set; } = new();

        public OverridableValue<SchedulingParameters> SchedulingParameters { get; private set; } = new();

        public ReadOnlyObservableCollection<IVirtualTeam> Teams { get; }

        public ReadOnlyObservableCollection<Fixture> Fixtures { get; }

        public ReadOnlyObservableCollection<RoundStage> Stages { get; }

        public MatchRules ProvideRules() => MatchRules.Value.OrThrow();

        public SchedulingParameters ProvideSchedulingParameters() => SchedulingParameters.Value.OrThrow();

        public IEnumerable<IVirtualTeam> ProvideTeams() => Teams.Union(Ancestor is not null ? Ancestor.Fixtures.Select(x => IsWinnerRound ? x.GetWinnerTeam() : x.GetLooserTeam()) : []);

        public IEnumerable<Match> GetAllMatches() => _stages.SelectMany(x => x.GetAllMatches());

        public IEnumerable<T> GetStages<T>() where T : IStage => Stages.OfType<T>().Union(Stages.SelectMany(x => x.GetStages<T>()));

        public bool RemoveMatch(Match item) => _stages.Any(x => x.RemoveMatch(item));

        public ExtendedResult GetExtendedResultOf(IVirtualTeam team) => Format.GetExtendedResultOf(this, team);

        public Result GetResultOf(IVirtualTeam team)
            => GetExtendedResultOf(team) switch
            {
                ExtendedResult.Won or ExtendedResult.WonAfterShootouts => Result.Won,
                ExtendedResult.Drawn => Result.Drawn,
                ExtendedResult.Lost or ExtendedResult.Withdrawn or ExtendedResult.LostAfterShootouts => Result.Lost,
                _ => Result.None,
            };

        public bool IsPlayed() => GetAllMatches().All(x => x.IsPlayed());

        #region Stages

        public bool CanAddStage() => Format.CanAddStage(this);

        public RoundStage AddStage(DateTime date) => AddStage(new RoundStage(this, date));

        public RoundStage AddStage(RoundStage stage)
        {
            if (!CanAddStage())
                throw new InvalidOperationException("Cannot add more stages");

            if (!ReferenceEquals(stage.Stage, this))
                throw new ArgumentException("Stage is not this round", nameof(stage));

            if (Stages.Contains(stage))
                throw new AlreadyExistsException(nameof(Stages), stage);

            _stages.Add(stage);

            Fixtures.ForEach(x => Format.CanAddFixtureInStage(stage, x).IfTrue(() => stage.AddMatch(x, invertTeams: Format.InvertTeams(stage))));

            return stage;
        }

        public virtual bool RemoveStage(RoundStage stage) => Format.CanRemoveStage(stage) && _stages.Remove(stage);

        #endregion

        #region Fixtures

        public virtual Fixture AddFixture(IVirtualTeam team1, IVirtualTeam team2, int? rank = null) => AddFixture(new Fixture(this, team1, team2, rank));

        public virtual Fixture AddFixture(Fixture fixture)
        {
            if (!ReferenceEquals(fixture.Stage, this))
                throw new ArgumentException("Stage is not this round", nameof(fixture));

            if (Fixtures.Contains(fixture))
                throw new AlreadyExistsException(nameof(Fixtures), fixture);

            _fixtures.Add(fixture);

            Stages.ForEach(x => Format.CanAddFixtureInStage(x, fixture).IfTrue(() => x.AddMatch(fixture, invertTeams: Format.InvertTeams(x))));

            return fixture;
        }

        public virtual bool RemoveFixture(Fixture item)
        {
            var matches = item.GetAllMatches().ToList();
            matches.ForEach(x => _stages.ForEach(y => y.RemoveMatch(x)));

            return _fixtures.Remove(item);
        }

        #endregion

        #region Teams

        public IVirtualTeam AddTeam(IVirtualTeam team)
        {
            if (Teams.Contains(team))
                throw new AlreadyExistsException(nameof(Teams), team);

            _teams.Add(team);

            return team;
        }

        public virtual bool RemoveTeam(IVirtualTeam team)
        {
            Fixtures.Where(x => x.Participate(team)).ToList().ForEach(y => RemoveFixture(y));
            return _teams.Remove(team);
        }

        #endregion
    }
}
