// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Collections;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class RoundOfFixtures : NameEntity, IRound, IFixtureFormatProvider
    {
        private readonly ExtendedObservableCollection<IVirtualTeam> _teams = [];
        private readonly ExtendedObservableCollection<RoundStage> _stages = [];
        private readonly ExtendedObservableCollection<Fixture> _fixtures = [];

        public RoundOfFixtures(Knockout stage,
                               string name,
                               string? shortName = null,
                               IFixtureResultStrategy? resultStrategy = null,
                               FixtureFormat? fixtureFormat = null,
                               MatchRules? matchRules = null,
                               SchedulingParameters? schedulingParameters = null,
                               Guid? id = null)
            : base(name, shortName, id)
        {
            Stage = stage;
            FixtureFormat = fixtureFormat ?? stage.ProvideFormat();
            MatchRules = matchRules ?? stage.ProvideRules();
            SchedulingParameters = schedulingParameters ?? stage.ProvideSchedulingParameters();
            Teams = new(_teams);
            Fixtures = new(_fixtures);
            Stages = new(_stages);
            ResultStrategy = resultStrategy ?? FixtureResultStrategy.Default;
        }

        public Knockout Stage { get; }

        public FixtureFormat FixtureFormat { get; set; }

        public IFixtureResultStrategy ResultStrategy { get; }

        public MatchRules MatchRules { get; set; }

        public SchedulingParameters SchedulingParameters { get; set; }

        public ReadOnlyObservableCollection<IVirtualTeam> Teams { get; }

        public ReadOnlyObservableCollection<Fixture> Fixtures { get; }

        public ReadOnlyObservableCollection<RoundStage> Stages { get; }

        FixtureFormat IFixtureFormatProvider.ProvideFormat() => FixtureFormat;

        MatchFormat IMatchFormatProvider.ProvideFormat() => FixtureFormat;

        MatchRules IMatchRulesProvider.ProvideRules() => MatchRules;

        SchedulingParameters ISchedulingParametersProvider.ProvideSchedulingParameters() => SchedulingParameters;

        IEnumerable<IVirtualTeam> ITeamsProvider.ProvideTeams() => Teams.AsEnumerable();

        public IEnumerable<Match> GetAllMatches() => _stages.SelectMany(x => x.GetAllMatches());

        public IEnumerable<T> GetStages<T>() where T : ICompetitionStage => Stages.OfType<T>().Union(Stages.SelectMany(x => x.GetStages<T>()));

        public bool RemoveMatch(Match item) => _stages.Any(x => x.RemoveMatch(item));

        #region Stages

        public RoundStage AddStage(DateTime date, string name, string? shortName = null, MatchFormat? matchFormat = null)
            => AddStage(new RoundStage(this, date, name, shortName, matchFormat ?? Stage.ProvideFormat()));

        public RoundStage AddStage(RoundStage stage)
        {
            if (!ReferenceEquals(stage.Stage, this))
                throw new ArgumentException("Stage is not this round", nameof(stage));

            if (Stages.Contains(stage))
                throw new AlreadyExistsException(nameof(Stages), stage);

            _stages.Add(stage);

            return stage;
        }

        public bool RemoveStages(RoundStage item) => _stages.Remove(item);

        public void Clear()
        {
            _fixtures.Clear();
            _stages.Clear();
        }

        #endregion

        #region Fixtures

        protected virtual Fixture AddFixture(Fixture fixture)
        {
            if (!ReferenceEquals(fixture.Stage, this))
                throw new ArgumentException("Stage is not this round", nameof(fixture));

            if (Fixtures.Contains(fixture))
                throw new AlreadyExistsException(nameof(Fixtures), fixture);

            _fixtures.Add(fixture);

            return fixture;
        }

        protected virtual bool RemoveFixture(Fixture item)
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
