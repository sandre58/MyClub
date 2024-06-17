// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using DynamicData.PLinq;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.TrainingAggregate;
using MyClub.Teamup.Wpf.Collections;
using MyClub.Teamup.Wpf.Enums;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using MyNet.DynamicData.Extensions;
using MyNet.Observable;
using MyNet.Observable.Statistics;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.Utilities;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class TrainingSessionViewModel : EntityViewModelBase<TrainingSession>, ISearchableItem, IAppointment
    {
        private readonly TrainingSessionPresentationService _trainingSessionPresentationService;
        private readonly HolidaysProvider _holidaysProvider;
        private readonly CyclesProvider _cyclesProvider;
        private readonly UiObservableCollection<TrainingAttendanceViewModel> _attendances = [];
        private readonly UiObservableCollection<string> _stages = [];
        private readonly UiObservableCollection<string> _technicalGoals = [];
        private readonly UiObservableCollection<string> _tacticalGoals = [];
        private readonly UiObservableCollection<string> _physicalGoals = [];
        private readonly UiObservableCollection<string> _mentalGoals = [];

        public TrainingSessionViewModel(TrainingSession item,
                                 TrainingSessionPresentationService trainingSessionPresentationService,
                                 PlayersProvider playersProvider,
                                 HolidaysProvider holidaysProvider,
                                 CyclesProvider cyclesProvider) : base(item)
        {
            _trainingSessionPresentationService = trainingSessionPresentationService;
            _holidaysProvider = holidaysProvider;
            _cyclesProvider = cyclesProvider;
            Attendances = new(_attendances);
            Stages = new(_stages);
            TechnicalGoals = new(_technicalGoals);
            TacticalGoals = new(_tacticalGoals);
            PhysicalGoals = new(_physicalGoals);
            MentalGoals = new(_mentalGoals);

            InitializeAttendancesCommand = CommandsManager.Create(async () => await InitializeAttendancesAsync().ConfigureAwait(false), CanInitializeAttendances);
            OpenCommand = CommandsManager.Create<TrainingSessionPageTab?>(Open);
            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            EditAttendancesCommand = CommandsManager.Create(async () => await EditAttendancesAsync().ConfigureAwait(false), () => !Item.IsCancelled);
            CancelCommand = CommandsManager.Create(async () => await CancelAsync().ConfigureAwait(false), CanCancel);
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
            DuplicateCommand = CommandsManager.Create(async () => await DuplicateAsync().ConfigureAwait(false));

            Presents = new(_attendances, x => x.Attendance == Attendance.Present, x => x.WhenPropertyChanged(y => y.Attendance));
            Absents = new(_attendances, x => x.Attendance == Attendance.Absent, x => x.WhenPropertyChanged(y => y.Attendance));
            AllAbsents = new(_attendances, x => x.Attendance is not Attendance.Present and not Attendance.Unknown, x => x.WhenPropertyChanged(y => y.Attendance));
            AllApologized = new(_attendances, x => x.Attendance is not Attendance.Present and not Attendance.Absent and not Attendance.Unknown, x => x.WhenPropertyChanged(y => y.Attendance));
            Apologized = new(_attendances, x => x.Attendance == Attendance.Apology, x => x.WhenPropertyChanged(y => y.Attendance));
            InHolidays = new(_attendances, x => x.Attendance == Attendance.InHolidays, x => x.WhenPropertyChanged(y => y.Attendance));
            InSelection = new(_attendances, x => x.Attendance == Attendance.InSelection, x => x.WhenPropertyChanged(y => y.Attendance));
            Injured = new(_attendances, x => x.Attendance == Attendance.Injured, x => x.WhenPropertyChanged(y => y.Attendance));
            Resting = new(_attendances, x => x.Attendance == Attendance.Resting, x => x.WhenPropertyChanged(y => y.Attendance));
            Unknown = new(_attendances, x => x.Attendance == Attendance.Unknown, x => x.WhenPropertyChanged(y => y.Attendance));
            Ratings = new(_attendances, x => x.Attendance == Attendance.Present && x.Rating.HasValue, x => x.Rating!.Value, x => x.WhenAnyPropertyChanged(nameof(TrainingAttendanceViewModel.Attendance), nameof(TrainingAttendanceViewModel.Rating))!);

            Disposables.AddRange(
            [
                Item.Stages.ToObservableChangeSet().Bind(_stages).Subscribe(),
                Item.TechnicalGoals.ToObservableChangeSet().Bind(_technicalGoals).Subscribe(),
                Item.TacticalGoals.ToObservableChangeSet().Bind(_tacticalGoals).Subscribe(),
                Item.PhysicalGoals.ToObservableChangeSet().Bind(_physicalGoals).Subscribe(),
                Item.MentalGoals.ToObservableChangeSet().Bind(_mentalGoals).Subscribe(),
                Item.Attendances.ToObservableChangeSet(x => x.Id)
                                .Transform(x => new TrainingAttendanceViewModel(x, this, playersProvider.GetByPlayerIdOrThrow(x.Player.Id), _trainingSessionPresentationService))
                                .BindItems(_attendances)
                                .DisposeMany()
                                .Subscribe(),
                Item.WhenPropertyChanged(x => x.Start).Subscribe(_ => RaisePropertyChanged(nameof(StartDate))),
                Item.WhenPropertyChanged(x => x.End).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(EndDate));
                    RaisePropertyChanged(nameof(IsPerformed));
                }),
                Item.WhenPropertyChanged(x => x.IsCancelled).Subscribe(_ => RaisePropertyChanged(nameof(IsPerformed))),
                Item.WhenPropertyChanged(x => x.Start).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(Cycle));
                    RaisePropertyChanged(nameof(Holidays));
                }),
                Item.TeamIds.ToObservableChangeSet().Subscribe(_ => RaisePropertyChanged(nameof(Teams))),
                TeamsCollection.MyTeams.Connect().WhenPropertyChanged(x => x.Order).Subscribe(_ => RaisePropertyChanged(nameof(Teams))),
                holidaysProvider.Connect().SubscribeMany(x => x.WhenAnyPropertyChanged(nameof(HolidaysViewModel.StartDate), nameof(HolidaysViewModel.EndDate)).Subscribe(_ => RaisePropertyChanged(nameof(Holidays)))).Subscribe(_ => RaisePropertyChanged(nameof(Holidays))),
                cyclesProvider.Connect().SubscribeMany(x => x.WhenAnyPropertyChanged(nameof(CycleViewModel.StartDate), nameof(CycleViewModel.EndDate)).Subscribe(_ => RaisePropertyChanged(nameof(Cycle)))).Subscribe(_ => RaisePropertyChanged(nameof(Cycle)))
            ]);
        }

        [AlsoNotifyFor(nameof(SearchDisplayName))]
        public string? Theme => Item.Theme;

        public string? Place => Item.Place;

        [AlsoNotifyFor(nameof(SearchDisplayName))]
        public DateTime StartDate => Item.Start.ToLocalTime();

        public DateTime EndDate => Item.End.ToLocalTime();

        public TimeSpan StartTime => StartDate.TimeOfDay;

        public TimeSpan EndTime => EndDate.TimeOfDay;

        public bool IsPerformed => !IsCancelled && EndDate < DateTime.Now;

        public bool IsCancelled => Item.IsCancelled;

        public IReadOnlyCollection<TeamViewModel> Teams => TeamsCollection.All.Items.Where(x => Item.TeamIds.Contains(x.Id)).OrderBy(x => x.Order).ToList().AsReadOnly();

        public ReadOnlyObservableCollection<TrainingAttendanceViewModel> Attendances { get; }

        public CycleViewModel? Cycle => _cyclesProvider.Items.FirstOrDefault(x => x.ContainsDate(StartDate));

        public HolidaysViewModel? Holidays => _holidaysProvider.Items.FirstOrDefault(x => x.ContainsDate(StartDate));

        public ReadOnlyObservableCollection<string> Stages { get; }

        public ReadOnlyObservableCollection<string> TechnicalGoals { get; }

        public ReadOnlyObservableCollection<string> TacticalGoals { get; }

        public ReadOnlyObservableCollection<string> PhysicalGoals { get; }

        public ReadOnlyObservableCollection<string> MentalGoals { get; }

        public CountStatistics<TrainingAttendanceViewModel> Presents { get; }

        public CountStatistics<TrainingAttendanceViewModel> Absents { get; }

        public CountStatistics<TrainingAttendanceViewModel> AllAbsents { get; }

        public CountStatistics<TrainingAttendanceViewModel> AllApologized { get; }

        public CountStatistics<TrainingAttendanceViewModel> Apologized { get; }

        public CountStatistics<TrainingAttendanceViewModel> InHolidays { get; }

        public CountStatistics<TrainingAttendanceViewModel> InSelection { get; }

        public CountStatistics<TrainingAttendanceViewModel> Injured { get; }

        public CountStatistics<TrainingAttendanceViewModel> Resting { get; }

        public CountStatistics<TrainingAttendanceViewModel> Unknown { get; }

        public RangeStatistics<TrainingAttendanceViewModel> Ratings { get; }

        public ICommand EditCommand { get; }

        public ICommand EditAttendancesCommand { get; }

        public ICommand OpenCommand { get; }

        public ICommand InitializeAttendancesCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand RemoveCommand { get; }

        public ICommand DuplicateCommand { get; }

        public async Task DuplicateAsync() => await _trainingSessionPresentationService.DuplicateAsync(this).ConfigureAwait(false);

        public async Task EditAsync() => await _trainingSessionPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task EditAttendancesAsync() => await _trainingSessionPresentationService.EditAttendancesAsync(this).ConfigureAwait(false);

        public async Task InitializeAttendancesAsync() => await _trainingSessionPresentationService.InitializeAttendancesAsync([this]).ConfigureAwait(false);

        public async Task CancelAsync() => await _trainingSessionPresentationService.CancelAsync([this]).ConfigureAwait(false);

        public async Task RemoveAsync() => await _trainingSessionPresentationService.RemoveAsync([this]).ConfigureAwait(false);

        public void Open(TrainingSessionPageTab? tab = null) => NavigationCommandsService.NavigateToTrainingSessionPage(this, tab);

        void ISearchableItem.Open() => Open();

        public bool CanInitializeAttendances() => !IsCancelled && !Attendances.Any();

        public bool CanCancel() => !IsCancelled;

        #region ISearchableItem

        public string SearchDisplayName => $"{StartDate:dddd d MMMM yyyy}";

        public string SearchText => $"{StartDate:dddd d MMMM yyyy} {Item.Theme}";

        public string SearchCategory => nameof(MyClubResources.Trainings);

        #endregion
    }
}
