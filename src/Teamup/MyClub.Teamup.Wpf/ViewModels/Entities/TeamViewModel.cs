// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Messages;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Messaging;
using MyNet.Wpf.Extensions;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class TeamViewModel : EntityViewModelBase<Team>, ISearchableItem
    {
        private readonly TeamPresentationService _teamPresentationService;
        private readonly StadiumsProvider _stadiumsProvider;

        public TeamViewModel(Team item, StadiumsProvider stadiumsProvider, TeamPresentationService teamPresentationService, bool isMyIteam) : base(item)
        {
            _stadiumsProvider = stadiumsProvider;
            _teamPresentationService = teamPresentationService;
            IsMyTeam = isMyIteam;

            OpenCommand = CommandsManager.Create(async () => await OpenAsync().ConfigureAwait(false));
            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(x => Item.Club.PropertyChanged += x, x => Item.Club.PropertyChanged -= x).Subscribe(x => RaisePropertyChanged(x.EventArgs.PropertyName)),
            ]);

            Messenger.Default.Register<StadiumsChangedMessage>(this, _ => RaisePropertyChanged(nameof(Stadium)));
        }

        public string ClubName => Item.Club.Name;

        public string ShortName => Item.ShortName;

        public string Name => Item.Name;

        public byte[]? Logo => Item.Club.Logo;

        public Color? HomeColor => Item.HomeColor.Value?.ToColor();

        public Color? AwayColor => Item.AwayColor.Value?.ToColor();

        public Country? Country => Item.Club.Country;

        public Category Category => Item.Category;

        public StadiumViewModel? Stadium => Item.Stadium.Value is not null ? _stadiumsProvider.Get(Item.Stadium.Value.Id) : null;

        public int Order => Item.Order;

        public bool IsMyTeam { get; }

        public bool IsMainTeam { get; private set; }

        public ICommand EditCommand { get; }

        public ICommand OpenCommand { get; }

        internal void SetMainTeam(bool value) => IsMainTeam = value;

        public async Task EditAsync() => await _teamPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task OpenAsync() => await EditAsync().ConfigureAwait(false);

        public override string ToString() => Name;

        public void Open() => new Action(async () => await OpenAsync().ConfigureAwait(false)).Invoke();

        #region ISearchableItem

        public string SearchDisplayName => Name;

        public string SearchText => Name;

        public string SearchCategory => nameof(MyClubResources.Teams);

        #endregion
    }
}
