// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class StadiumViewModel : EntityViewModelBase<Stadium>, IStadiumViewModel
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

        public string GroundImagePath => $"pack://application:,,,/Scorer;component/Resources/Images/{Ground}.jpg";

        public Address? Address => Item.Address;

        public ICommand EditCommand { get; }

        public ICommand OpenCommand { get; }

        public ICommand OpenGoogleMapsCommand { get; }

        public async Task EditAsync() => await _stadiumPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task OpenAsync() => await _stadiumPresentationService.OpenAsync(this).ConfigureAwait(false);
    }

    internal interface IStadiumWrapper : INotifyPropertyChanged
    {
        string? Header { get; }
    }

    internal class StadiumWrapper : ObservableObject, IStadiumWrapper, IIdentifiable<Guid>
    {
        public AvailabilityCheck Availability { get; set; }

        public IStadiumViewModel Stadium { get; }

        public Guid Id => Stadium.Id;

        public string? Header => Stadium.Address?.City;

        public StadiumWrapper(IStadiumViewModel item) => Stadium = item;
    }

    internal class AutomaticStadiumWrapper : EditableObject, IStadiumWrapper
    {
        public string? Header => null;
    }

    internal class NoStadiumWrapper : EditableObject, IStadiumWrapper
    {
        public string? Header => null;
    }
}
