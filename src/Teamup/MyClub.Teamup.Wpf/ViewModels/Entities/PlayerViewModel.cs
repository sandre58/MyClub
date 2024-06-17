// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using DynamicData.PLinq;
using MyClub.CrossCutting.Localization;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Wpf.Collections;
using MyClub.Teamup.Wpf.Enums;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Humanizer;
using MyNet.Observable.Attributes;
using MyNet.UI.Threading;
using MyNet.Observable.Translatables;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Geography.Extensions;
using MyNet.Utilities.Google.Maps;
using MyNet.Utilities.Units;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class PlayerViewModel : EntityViewModelBase<SquadPlayer>, ISearchableItem
    {
        private readonly PlayerPresentationService _playerPresentationService;

        private readonly ObservableCollectionExtended<InjuryViewModel> _injuries = [];
        private readonly ObservableCollectionExtended<AbsenceViewModel> _absences = [];
        private readonly ObservableCollectionExtended<RatedPositionViewModel> _positions = [];
        private readonly ObservableCollectionExtended<Email> _emails = [];
        private readonly ObservableCollectionExtended<Phone> _phones = [];
        private readonly ObservableCollectionExtended<TeamViewModel> _otherTeams = [];

        public PlayerViewModel(SquadPlayer item, PlayerPresentationService playerPresentationService) : base(item)
        {
            _playerPresentationService = playerPresentationService;
            OtherTeams = new(_otherTeams);
            Injuries = new(_injuries);
            Absences = new(_absences);
            Positions = new(_positions);
            Emails = new(_emails);
            Phones = new(_phones);
            InjuryStatistics = new(this);
            TrainingStatistics = new(this);

            OpenCommand = CommandsManager.Create<PlayerPageTab?>(Open);
            OpenInjuriesCommand = CommandsManager.Create(() => Open(PlayerPageTab.Injuries));
            OpenTrainingsCommand = CommandsManager.Create(() => Open(PlayerPageTab.Trainings));
            OpenAbsencesCommand = CommandsManager.Create(() => Open(PlayerPageTab.Absences));
            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
            MoveCommand = CommandsManager.Create<TeamViewModel>(async x => await MoveAsync(x).ConfigureAwait(false), x => !Equals(x?.Id, TeamId));
            OpenMailClientCommand = CommandsManager.Create(OpenMailClient, () => Email is not null);
            OpenEmailInMailClientCommand = CommandsManager.CreateNotNull<string>(OpenMailClient);
            OpenGoogleMapsCommand = CommandsManager.Create(() => Address?.OpenInGoogleMaps(), () => Address is not null);
            AddInjuryCommand = CommandsManager.Create(async () => await AddInjuryAsync().ConfigureAwait(false));
            AddAbsenceHolidaysCommand = CommandsManager.Create(async () => await AddAbsenceAsync(AbsenceType.InHolidays).ConfigureAwait(false));
            AddAbsenceInSelectionCommand = CommandsManager.Create(async () => await AddAbsenceAsync(AbsenceType.InSelection).ConfigureAwait(false));
            AddAbsenceOtherCommand = CommandsManager.Create(async () => await AddAbsenceAsync(AbsenceType.Other).ConfigureAwait(false));

            var teamFilterSubject = new Subject<Guid?>();
            var otherTeamDynamicFilter = teamFilterSubject.Select(x => new Func<TeamViewModel, bool>(y => x != y.Id));

            Disposables.AddRange(
            [
                Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(x => Item.Player.PropertyChanged += x, x => Item.Player.PropertyChanged -= x).Subscribe(x => RaisePropertyChanged(x.EventArgs.PropertyName)),
                Item.WhenAnyPropertyChanged(nameof(Player.LastName), nameof(Player.FirstName)).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(InverseName));
                    RaisePropertyChanged(nameof(FullName));
                    RaisePropertyChanged(nameof(FirstLetter));
                    RaisePropertyChanged(nameof(SearchDisplayName));
                }),
                Item.WhenPropertyChanged(x => x.Team).Subscribe(x =>
                {
                    RaisePropertyChanged(nameof(Team));
                    RaisePropertyChanged(nameof(TeamId));
                    teamFilterSubject.OnNext(x.Value?.Id);
                }),
                Item.WhenPropertyChanged(x => x.Player.Birthdate).Subscribe(_ => RaisePropertyChanged(nameof(Age))),
                Item.WhenPropertyChanged(x => x.Player.Address).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(FullAddress));
                    RaisePropertyChanged(nameof(GoogleMapsAddressUrl));
                }),
                Item.WhenPropertyChanged(x => x.Player.Height).Subscribe(x => Height.Value = x.Value),
                Item.WhenPropertyChanged(x => x.Player.Weight).Subscribe(x => Weight.Value = x.Value),
                Item.Player.Injuries.ToObservableChangeSet(x => x.Id).Transform(x => new InjuryViewModel(x, this, playerPresentationService)).ObserveOn(Scheduler.UI).Bind(_injuries).DisposeMany().Subscribe(),
                Item.Player.Absences.ToObservableChangeSet(x => x.Id).Transform(x => new AbsenceViewModel(x, this, playerPresentationService)).ObserveOn(Scheduler.UI).Bind(_absences).DisposeMany().Subscribe(),
                Item.Positions.ToObservableChangeSet(x => x.Id).Transform(x => new RatedPositionViewModel(x, this)).ObserveOn(Scheduler.UI).Bind(_positions).DisposeMany().Subscribe(),
                Item.Player.Phones.ToObservableChangeSet().Sort(SortExpressionComparer<Phone>.Descending(x => x.Default)).ObserveOn(Scheduler.UI).Bind(_phones).Subscribe(_ => RaisePropertyChanged(nameof(Phone))),
                Item.Player.Emails.ToObservableChangeSet().Sort(SortExpressionComparer<Email>.Descending(x => x.Default)).ObserveOn(Scheduler.UI).Bind(_emails).Subscribe(_ => RaisePropertyChanged(nameof(Email))),
                _injuries.ToObservableChangeSet(x => x.Id).SubscribeMany(x => x.WhenPropertyChanged(x => x.IsCurrent).Subscribe(_ => RaiseInjuryProperties())).Subscribe(_ => RaiseInjuryProperties()),
                _absences.ToObservableChangeSet(x => x.Id).SubscribeMany(x => x.WhenPropertyChanged(x => x.IsCurrent).Subscribe(_ => RaiseAbsenceProperties())).Subscribe(_ => RaiseAbsenceProperties()),
                _positions.ToObservableChangeSet(x => x.Id).SubscribeMany(x => x.WhenAnyPropertyChanged().Subscribe(_ => RaisePositionsProperties())).Subscribe(_ => RaisePositionsProperties()),
                TeamsCollection.MyTeams.Connect().AutoRefreshOnObservable(x => x.WhenPropertyChanged(y => y.Order)).Filter(otherTeamDynamicFilter).Sort(SortExpressionComparer<TeamViewModel>.Ascending(x => x.Order)).ObserveOn(Scheduler.UI).Bind(_otherTeams).Subscribe()
            ]);

            teamFilterSubject.OnNext(Item.Team?.Id);
        }

        public PlayerInjuryStatisticsViewModel InjuryStatistics { get; }

        public PlayerTrainingStatisticsViewModel TrainingStatistics { get; }

        public Guid PlayerId => Item.Player.Id;

        public string LastName => Item.Player.LastName;

        public string FirstName => Item.Player.FirstName;

        public DateTime? Birthdate => Item.Player.Birthdate?.Date;

        public DateTime? FromDate => Item.FromDate?.Date;

        public string? PlaceOfBirth => Item.Player.PlaceOfBirth;

        public Country? Country => Item.Player.Country;

        public byte[]? Photo => Item.Player.Photo;

        public GenderType Gender => Item.Player.Gender;

        public Address? Address => Item.Player.Address;

        [UpdateOnCultureChanged]
        public string FullAddress => new List<string?> { Address?.Street, Address?.PostalCode, Address?.City, Address?.Country?.GetDisplayName() }.Humanize(" ");

        public string? GoogleMapsAddressUrl => GoogleMapsHelper.GetGoogleMapsUrl(new GoogleMapsSettings { Address = FullAddress, HideLeftPanel = true });

        public string? LicenseNumber => Item.Player.LicenseNumber;

        public string? Description => Item.Player.Description;

        public string? Size => Item.Size;

        public Guid? TeamId => Item.Team?.Id;

        public TeamViewModel? Team => Item.Team is not null ? TeamsCollection.All.GetByIdOrDefault(Item.Team.Id) : null;

        public Category? Category => Item.Category;

        public Laterality Laterality => Item.Player.Laterality;

        public Length<int> Height { get; } = new(LengthUnit.Centimeter);

        public Mass<int> Weight { get; } = new(MassUnit.Kilogram);

        public int? ShoesSize => Item.ShoesSize;

        public LicenseState LicenseState => Item.LicenseState;

        public bool IsMutation => Item.IsMutation;

        public int? Number => Item.Number;

        public ReadOnlyObservableCollection<Email> Emails { get; }

        public ReadOnlyObservableCollection<Phone> Phones { get; }

        public ReadOnlyObservableCollection<TeamViewModel> OtherTeams { get; }

        public ReadOnlyObservableCollection<RatedPositionViewModel> Positions { get; }

        public ReadOnlyObservableCollection<InjuryViewModel> Injuries { get; }

        public ReadOnlyObservableCollection<AbsenceViewModel> Absences { get; }

        public AbsenceViewModel? Absence => Absences.FirstOrDefault(x => x.IsCurrent);

        public AbsenceViewModel? NextAbsence => Absences.OrderBy(x => x.StartDate).FirstOrDefault(x => x.StartDate > DateTime.Today);

        public InjuryViewModel? Injury => Injuries.FirstOrDefault(x => x.IsCurrent);

        public Phone? Phone => Phones.OrderByDescending(x => x.Default).FirstOrDefault();

        public Email? Email => Emails.OrderByDescending(x => x.Default).FirstOrDefault();

        public IEnumerable<Position> NaturalPositions => Positions.Where(x => x.IsNatural).Select(x => x.Position);

        public Position? NaturalPosition => NaturalPositions is null || !NaturalPositions.Any() ? null : NaturalPositions.First();

        public IReadOnlyCollection<Position> GoodPositions => new ReadOnlyCollection<Position>(GetGoodPositions());

        public IReadOnlyCollection<Position> BestPositions => new ReadOnlyCollection<Position>(GetBestPositions());

        public bool IsAbsent => Absence is not null;

        public bool IsInjured => Injury is not null;

        public string InverseName => Item.Player.GetInverseName();

        public string FullName => Item.Player.GetFullName();

        public string FirstLetter => Item.Player.LastName.Substring(0, 1).ToUpperInvariant();

        public int? Age => Item.Player.GetAge();

        public int CountInformations
        {
            get
            {
                var result = 0;
                if (IsAbsent) result++;
                if (IsInjured) result++;

                return result;
            }
        }

        public ICommand EditCommand { get; }

        public ICommand RemoveCommand { get; }

        public ICommand MoveCommand { get; }

        public ICommand OpenCommand { get; }

        public ICommand OpenInjuriesCommand { get; }

        public ICommand OpenTrainingsCommand { get; }

        public ICommand OpenAbsencesCommand { get; }

        public ICommand OpenMailClientCommand { get; }

        public ICommand OpenEmailInMailClientCommand { get; }

        public ICommand OpenGoogleMapsCommand { get; }

        public ICommand AddInjuryCommand { get; }

        public ICommand AddAbsenceHolidaysCommand { get; }

        public ICommand AddAbsenceInSelectionCommand { get; }

        public ICommand AddAbsenceOtherCommand { get; }

        private void RaiseInjuryProperties()
        {
            RaisePropertyChanged(nameof(Injury));
            RaisePropertyChanged(nameof(IsInjured));
            RaisePropertyChanged(nameof(CountInformations));
        }

        private void RaiseAbsenceProperties()
        {
            RaisePropertyChanged(nameof(Absence));
            RaisePropertyChanged(nameof(IsAbsent));
            RaisePropertyChanged(nameof(NextAbsence));
            RaisePropertyChanged(nameof(CountInformations));
        }

        private void RaisePositionsProperties()
        {
            RaisePropertyChanged(nameof(Positions));
            RaisePropertyChanged(nameof(NaturalPositions));
            RaisePropertyChanged(nameof(NaturalPosition));
            RaisePropertyChanged(nameof(GoodPositions));
        }

        public async Task EditAsync() => await _playerPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task RemoveAsync() => await _playerPresentationService.RemoveAsync([this]).ConfigureAwait(false);

        public async Task MoveAsync(TeamViewModel? team) => await _playerPresentationService.MoveAsync(this, team).ConfigureAwait(false);

        public async Task<InjuryViewModel?> AddInjuryAsync() => await _playerPresentationService.AddInjuryAsync(this).ConfigureAwait(false);

        public async Task AddAbsenceAsync(AbsenceType type = AbsenceType.Other, DateTime? startDate = null, DateTime? endDate = null)
             => await _playerPresentationService.AddAbsenceAsync(this, type, startDate, endDate).ConfigureAwait(false);

        public void Open(PlayerPageTab? tab = null) => NavigationCommandsService.NavigateToPlayerPage(this, tab);

        void ISearchableItem.Open() => Open();

        public void OpenMailClient()
        {
            if (Email is not null)
                OpenMailClient(Email.Value);
        }

        private static void OpenMailClient(string email) => MailCommandsService.OpenMailClient([email]);

        public bool IsAbsentAtDate(DateTime dateTime) => Item.Player.IsAbsentAtDate(dateTime);

        public bool IsInjuredAtDate(DateTime dateTime) => Item.Player.IsInjuredAtDate(dateTime);

        private List<Position> GetGoodPositions() => Positions.Where(x => (int)x.Rating >= (int)PositionRating.Natural - 2 && !x.IsNatural).Select(x => x.Position).ToList();

        private List<Position> GetBestPositions() => Positions.Where(x => (int)x.Rating >= (int)PositionRating.Natural - 2).Select(x => x.Position).ToList();

        protected override void Cleanup()
        {
            TrainingStatistics.Dispose();
            InjuryStatistics.Dispose();
            base.Cleanup();
        }

        #region ISearchableItem

        public string SearchDisplayName => FullName;

        public string SearchText => FullName;

        public string SearchCategory => nameof(MyClubResources.Players);

        #endregion
    }
}
