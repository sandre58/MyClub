// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyClub.Teamup.Domain.MatchAggregate;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class GroupStage : Round<ChampionshipRules>, IHasMatchdays
    {
        private readonly ObservableCollection<Matchday> _matchdays = [];
        private readonly ObservableCollection<Group> _groups = [];

        public GroupStage(string name, string shortName, DateTime startDate, DateTime endDate, ChampionshipRules? rules = null, Guid? id = null)
            : base(name, shortName, rules ?? new ChampionshipRules(), id)
        {
            Period = new(startDate, endDate);
            Groups = new(_groups);
            Matchdays = new(_matchdays);
        }

        public ReadOnlyObservableCollection<Group> Groups { get; }

        public ReadOnlyObservableCollection<Matchday> Matchdays { get; }

        public ObservablePeriod Period { get; }

        public Group AddGroup(string name, string? shortName = null) => AddGroup(new Group(this, name, shortName ?? name.GetInitials()));

        public Group AddGroup(Group matchday)
        {
            _groups.Add(matchday);

            return matchday;
        }

        public void SetGroups(IEnumerable<Group> groups) => _groups.UpdateFrom(groups, x => AddGroup(x), x => RemoveGroup(x), (x, y) =>
        {
            x.Name = y.Name;
            x.ShortName = y.ShortName;
            x.Order = y.Order;
            x.Penalties = y.Penalties;
            x.SetTeams(y.Teams);

        }, (x, y) => x.Id == y.Id);

        public bool RemoveGroup(Group matchday) => _groups.Remove(matchday);

        public bool RemoveGroup(Guid matchdayId) => _groups.HasId(matchdayId) && _groups.Remove(_groups.GetById(matchdayId));

        public void ClearGroups() => _groups.Clear();

        public override IEnumerable<Match> GetAllMatches() => Matchdays.SelectMany(x => x.Matches);

        public override bool RemoveMatch(Match match) => Matchdays.Any(x => x.RemoveMatch(match));

        public Matchday AddMatchday(string name, DateTime date, string? shortName = null)
        {
            var datetime = date.ToLocalTime().TimeOfDay == TimeSpan.Zero ? date.AddFluentTimeSpan(Rules.MatchTime) : date;
            return AddMatchday(new Matchday(name, datetime, shortName, Rules.MatchFormat));
        }

        public Matchday AddMatchday(Matchday matchday)
        {
            _matchdays.Add(matchday);

            return matchday;
        }

        public bool RemoveMatchday(Matchday matchday) => _matchdays.Remove(matchday);

        public bool RemoveMatchday(Guid matchdayId) => _matchdays.HasId(matchdayId) && _matchdays.Remove(_matchdays.GetById(matchdayId));

        public void ClearMatchdays() => _matchdays.Clear();
    }
}

