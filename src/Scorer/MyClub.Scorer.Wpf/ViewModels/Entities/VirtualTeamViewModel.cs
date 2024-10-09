// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable.Attributes;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal abstract class VirtualTeamViewModel<T> : EntityViewModelBase<T>, IVirtualTeamViewModel where T : IVirtualTeam
    {
        public VirtualTeamViewModel(T item) : base(item)
        {
        }

        [UpdateOnCultureChanged]
        public string Name => GetDisplayName();

        [UpdateOnCultureChanged]
        public string ShortName => GetDisplayShortName();

        protected abstract string GetDisplayName();

        protected abstract string GetDisplayShortName();

        public override string ToString() => GetDisplayName();
    }

    internal class WinnerOfMatchTeamViewModel : VirtualTeamViewModel<IVirtualTeam>
    {
        public WinnerOfMatchTeamViewModel(IVirtualTeam item, MatchViewModel match) : base(item) => Match = match;

        public MatchViewModel Match { get; }

        protected override string GetDisplayName() => MyClubResources.WinnerOfX.FormatWith(Match.ShortName);

        protected override string GetDisplayShortName() => MyClubResources.WinnerOfXAbbr.FormatWith(Match.ShortName);
    }

    internal class LooserOfMatchTeamViewModel : VirtualTeamViewModel<IVirtualTeam>
    {
        public LooserOfMatchTeamViewModel(IVirtualTeam item, MatchViewModel match) : base(item) => Match = match;

        public MatchViewModel Match { get; }

        protected override string GetDisplayName() => MyClubResources.LooserOfX.FormatWith(Match.ShortName);

        protected override string GetDisplayShortName() => MyClubResources.LooserXAbbr.FormatWith(Match.ShortName);
    }

    internal class WinnerOfFixtureTeamViewModel : VirtualTeamViewModel<IVirtualTeam>
    {
        public WinnerOfFixtureTeamViewModel(IVirtualTeam item, FixtureViewModel fixture) : base(item) => Fixture = fixture;

        public FixtureViewModel Fixture { get; }

        protected override string GetDisplayName() => MyClubResources.WinnerOfX.FormatWith(Fixture.ShortName);

        protected override string GetDisplayShortName() => MyClubResources.WinnerOfXAbbr.FormatWith(Fixture.ShortName);
    }

    internal class LooserOfFixtureTeamViewModel : VirtualTeamViewModel<IVirtualTeam>
    {
        public LooserOfFixtureTeamViewModel(IVirtualTeam item, FixtureViewModel fixture) : base(item) => Fixture = fixture;

        public FixtureViewModel Fixture { get; }

        protected override string GetDisplayName() => MyClubResources.LooserOfX.FormatWith(Fixture.ShortName);

        protected override string GetDisplayShortName() => MyClubResources.LooserXAbbr.FormatWith(Fixture.ShortName);
    }
}
