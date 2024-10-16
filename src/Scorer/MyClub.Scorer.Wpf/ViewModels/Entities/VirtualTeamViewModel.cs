// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.TeamAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.Commands;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal abstract class VirtualTeamViewModel<T> : EntityViewModelBase<T>, IVirtualTeamViewModel where T : IVirtualTeam
    {
        public VirtualTeamViewModel(T item) : base(item) => OpenCommand = CommandsManager.Create(Open, CanOpen);

        public string Name => GetDisplayName();

        public string ShortName => GetDisplayShortName();

        public ICommand OpenCommand { get; }

        protected abstract string GetDisplayName();

        protected abstract string GetDisplayShortName();

        protected abstract void Open();

        protected virtual bool CanOpen() => true;

        public abstract TeamViewModel? ProvideTeam();

        public override string ToString() => GetDisplayName();
    }

    internal class WinnerOfMatchTeamViewModel : VirtualTeamViewModel<IVirtualTeam>
    {
        public WinnerOfMatchTeamViewModel(IVirtualTeam item, MatchViewModel match) : base(item)
        {
            Match = match;

            Disposables.AddRange(
             [
                match.WhenPropertyChanged(x => x.DisplayName).Subscribe(_ => RaisePropertyChanged(nameof(Name))),
                match.WhenPropertyChanged(x => x.DisplayShortName).Subscribe(_ => RaisePropertyChanged(nameof(ShortName)))
             ]);
        }

        public MatchViewModel Match { get; }

        protected override string GetDisplayName() => MyClubResources.WinnerOfX.FormatWith(Match.DisplayShortName);

        protected override string GetDisplayShortName() => MyClubResources.WinnerOfXAbbr.FormatWith(Match.DisplayShortName);

        protected override async void Open() => await Match.OpenAsync().ConfigureAwait(false);

        public override TeamViewModel? ProvideTeam() => Match.GetWinner();
    }

    internal class LooserOfMatchTeamViewModel : VirtualTeamViewModel<IVirtualTeam>
    {
        public LooserOfMatchTeamViewModel(IVirtualTeam item, MatchViewModel match) : base(item)
        {
            Match = match;

            Disposables.AddRange(
             [
                match.WhenPropertyChanged(x => x.DisplayName).Subscribe(_ => RaisePropertyChanged(nameof(Name))),
                match.WhenPropertyChanged(x => x.DisplayShortName).Subscribe(_ => RaisePropertyChanged(nameof(ShortName)))
             ]);
        }

        public MatchViewModel Match { get; }

        protected override string GetDisplayName() => MyClubResources.LooserOfX.FormatWith(Match.DisplayShortName);

        protected override string GetDisplayShortName() => MyClubResources.LooserXAbbr.FormatWith(Match.DisplayShortName);

        protected override async void Open() => await Match.OpenAsync().ConfigureAwait(false);

        public override TeamViewModel? ProvideTeam() => Match.GetLooser();
    }

    internal class WinnerOfFixtureTeamViewModel : VirtualTeamViewModel<IVirtualTeam>
    {
        public WinnerOfFixtureTeamViewModel(IVirtualTeam item, FixtureViewModel fixture) : base(item) => Fixture = fixture;

        public FixtureViewModel Fixture { get; }

        protected override string GetDisplayName() => MyClubResources.WinnerOfX.FormatWith(Fixture.ShortName);

        protected override string GetDisplayShortName() => MyClubResources.WinnerOfXAbbr.FormatWith(Fixture.ShortName);

        protected override void Open() { }

        public override TeamViewModel? ProvideTeam() => Fixture.GetWinner();
    }

    internal class LooserOfFixtureTeamViewModel : VirtualTeamViewModel<IVirtualTeam>
    {
        public LooserOfFixtureTeamViewModel(IVirtualTeam item, FixtureViewModel fixture) : base(item) => Fixture = fixture;

        public FixtureViewModel Fixture { get; }

        protected override string GetDisplayName() => MyClubResources.LooserOfX.FormatWith(Fixture.ShortName);

        protected override string GetDisplayShortName() => MyClubResources.LooserXAbbr.FormatWith(Fixture.ShortName);

        protected override void Open() { }

        public override TeamViewModel? ProvideTeam() => Fixture.GetLooser();
    }
}
