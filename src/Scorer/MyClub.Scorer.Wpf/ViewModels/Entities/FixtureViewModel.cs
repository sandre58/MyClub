// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.DynamicData.Extensions;
using MyNet.Observable.Attributes;
using MyNet.UI.Threading;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class FixtureViewModel : EntityViewModelBase<Fixture>
    {
        private readonly TeamsProvider _teamsProvider;
        private readonly ExtendedObservableCollection<MatchViewModel> _matches = [];

        public FixtureViewModel(Fixture item, RoundOfFixturesViewModel stage, TeamsProvider teamsProvider) : base(item)
        {
            _teamsProvider = teamsProvider;
            Matches = new(_matches);
            Stage = stage;
            WinnerTeam = new WinnerOfFixtureTeamViewModel(item.GetWinnerTeam(), this);
            LooserTeam = new LooserOfFixtureTeamViewModel(item.GetLooserTeam(), this);
            Team1 = teamsProvider.GetVirtualTeam(Item.Team1);
            Team2 = teamsProvider.GetVirtualTeam(Item.Team2);

            Disposables.Add(stage.Matches.ToObservableChangeSet().Filter(x => item.GetAllMatches().Select(y => y.Id).Contains(x.Id)).ObserveOn(Scheduler.GetUIOrCurrent()).Bind(_matches).Subscribe());
        }

        public RoundOfFixturesViewModel Stage { get; }

        public IVirtualTeamViewModel Team1 { get; }

        public IVirtualTeamViewModel Team2 { get; }

        public WinnerOfFixtureTeamViewModel WinnerTeam { get; }

        public LooserOfFixtureTeamViewModel LooserTeam { get; }

        public ReadOnlyObservableCollection<MatchViewModel> Matches { get; }

        [UpdateOnCultureChanged]
        public string Name => (Stage.Fixtures.IndexOf(this) + 1).ToString(MyClubResources.FixtureX);

        [UpdateOnCultureChanged]
        public string ShortName => (Stage.Fixtures.IndexOf(this) + 1).ToString(MyClubResources.FixtureXAbbr);

        [UpdateOnCultureChanged]
        public string DisplayName => $"{Stage.Name}{Name}";

        [UpdateOnCultureChanged]
        public string DisplayShortName => $"{Stage.ShortName}{ShortName}";

        public bool Participate(IVirtualTeamViewModel team) => Item.Participate(team.Id);

        public Result GetResultOf(TeamViewModel team) => Item.GetResultOf(team.Id);

        public ExtendedResult GetDetailledResultOf(TeamViewModel team) => Item.GetExtendedResultOf(team.Id);

        public TeamViewModel? GetWinner() => Item.GetWinner() is Team team ? _teamsProvider.Get(team.Id) : null;

        public TeamViewModel? GetLooser() => Item.GetLooser() is Team team ? _teamsProvider.Get(team.Id) : null;

        public bool IsWonBy(TeamViewModel team) => Item.IsWonBy(team.Id);

        public bool IsLostBy(TeamViewModel team) => Item.IsLostBy(team.Id);

        public bool IsPlayed() => Item.IsPlayed();
    }
}
