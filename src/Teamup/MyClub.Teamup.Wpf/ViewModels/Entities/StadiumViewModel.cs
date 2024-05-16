// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class StadiumViewModel : EntityViewModelBase<Stadium>, ISearchableItem
    {
        private readonly StadiumPresentationService _stadiumPresentationService;

        public StadiumViewModel(Stadium item, StadiumPresentationService stadiumPresentationService) : base(item)
        {
            _stadiumPresentationService = stadiumPresentationService;

            OpenCommand = CommandsManager.Create(async () => await OpenAsync().ConfigureAwait(false));
            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            OpenGoogleMapsCommand = CommandsManager.Create(() => Address?.OpenInGoogleMaps(), () => Address is not null);

            Disposables.AddRange(
            [
                item.WhenAnyPropertyChanged(nameof(Stadium.Name), nameof(Stadium.Address)).Subscribe(_ => RaisePropertyChanged(nameof(DisplayName))),
                item.WhenPropertyChanged(x => x.Ground).Subscribe(_ => RaisePropertyChanged(nameof(GroundImagePath))),
            ]);
        }

        public string Name => Item.Name;

        public string DisplayName => string.Join(", ", new[] { Name, Address?.City }.NotNull());

        public Ground Ground => Item.Ground;

        public string GroundImagePath => $"pack://application:,,,/Teamup;component/Resources/Images/{Ground}.jpg";

        public Address? Address => Item.Address;

        public ICommand EditCommand { get; }

        public ICommand OpenCommand { get; }

        public ICommand OpenGoogleMapsCommand { get; }

        public async Task EditAsync() => await _stadiumPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task OpenAsync() => await EditAsync().ConfigureAwait(false);

        public void Open() => new Action(async () => await OpenAsync().ConfigureAwait(false)).Invoke();

        #region ISearchableItem

        public string SearchDisplayName => DisplayName;

        public string SearchText => DisplayName;

        public string SearchCategory => nameof(MyClubResources.Stadiums);

        #endregion
    }
}
