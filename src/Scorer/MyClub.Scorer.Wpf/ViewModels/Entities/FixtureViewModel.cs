// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Observable.Attributes;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class FixtureViewModel : EntityViewModelBase<Fixture>
    {
        public FixtureViewModel(Fixture item, RoundOfFixturesViewModel stage) : base(item)
        {
            Stage = stage;
            WinnerTeam = new WinnerOfFixtureTeamViewModel(item.GetWinnerTeam(), this);
            LooserTeam = new LooserOfFixtureTeamViewModel(item.GetLooserTeam(), this);
        }

        public RoundOfFixturesViewModel Stage { get; }

        public WinnerOfFixtureTeamViewModel WinnerTeam { get; }

        public LooserOfFixtureTeamViewModel LooserTeam { get; }

        [UpdateOnCultureChanged]
        public string Name => (Stage.Fixtures.IndexOf(this) + 1).ToString(MyClubResources.FixtureX);

        [UpdateOnCultureChanged]
        public string ShortName => (Stage.Fixtures.IndexOf(this) + 1).ToString(MyClubResources.FixtureXAbbr);

        [UpdateOnCultureChanged]
        public string DisplayName => $"{Stage.Name}{Name}";

        [UpdateOnCultureChanged]
        public string DisplayShortName => $"{Stage.ShortName}{ShortName}";
    }
}
