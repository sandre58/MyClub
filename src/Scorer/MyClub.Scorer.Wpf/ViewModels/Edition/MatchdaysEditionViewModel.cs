// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.Factories;
using MyClub.Scorer.Wpf.Services.Deferrers;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class MatchdaysEditionViewModel : EditionViewModel
    {
        private readonly UiObservableCollection<MatchdayViewModel> _availableMatchdays = [];
        private readonly StadiumsProvider _stadiumsProvider;
        private readonly MatchdayService _matchdayService;
        private readonly ScheduleChangedDeferrer _scheduleChangedDeferrer;

        public MatchdaysEditionViewModel(CompetitionInfoProvider competitionInfoProvider,
                                         StadiumsProvider stadiumsProvider,
                                         MatchdayService matchdayService,
                                         ScheduleChangedDeferrer scheduleChangedDeferrer)
        {
            _stadiumsProvider = stadiumsProvider;
            _matchdayService = matchdayService;
            _scheduleChangedDeferrer = scheduleChangedDeferrer;
            AvailableMatchdays = new(_availableMatchdays);

            AddToDateCommand = CommandsManager.CreateNotNull<DateTime>(x => AddToDate(x), x => new Period(StartDisplayDate.GetValueOrDefault(), EndDisplayDate.GetValueOrDefault()).Contains(x));
            RemoveFromDateCommand = CommandsManager.CreateNotNull<DateTime>(x => Remove(Matchdays.LastOrDefault(y => y.Item.Date == x)), x => Matchdays.Any(y => y.Item.Date == x));
            RemoveCommand = CommandsManager.CreateNotNull<EditableMatchdayWrapper>(Remove);
            GenerateCommand = CommandsManager.Create(async () => await GenerateAsync().ConfigureAwait(false));
            RegenerateCommand = CommandsManager.Create(async () => await GenerateAsync(true).ConfigureAwait(false));
            ClearCommand = CommandsManager.Create(Clear, () => Matchdays.Count > 0);
            CollapseAllCommand = CommandsManager.Create(() => Matchdays.ForEach(x => x.IsExpanded = false), () => Matchdays.Any(x => x.IsExpanded));
            ExpandAllCommand = CommandsManager.Create(() => Matchdays.ForEach(x => x.IsExpanded = true), () => Matchdays.Any(x => !x.IsExpanded));
            InvertTeamsCommand = CommandsManager.Create(() => Matchdays.ForEach(x => x.Item.InvertTeams()), () => Matchdays.Any(x => x.Item.Matches.Count > 0));
            AddMatchesCommand = CommandsManager.Create(() => Matchdays.ForEach(x => EnumerableHelper.Iteration(MatchesToAdd!.Value, _ => x.Item.AddMatch())), () => Matchdays.Count > 0 && MatchesToAdd > 0);

            competitionInfoProvider.WhenCompetitionChanged(_ => Reset());
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public IMatchdayParent? Parent { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<MatchdayViewModel> AvailableMatchdays { get; }

        public UiObservableCollection<EditableMatchdayWrapper> Matchdays { get; } = [];

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public DateTime? StartDisplayDate { get; private set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public DateTime? EndDisplayDate { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public DisplayModeYear DateSelection { get; } = new();

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public MatchdaysEditionAutomaticViewModel AutomaticViewModel { get; set; } = new();

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public string? NamePattern { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public string? ShortNamePattern { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public TimeSpan? DefaultTime { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public int NextIndex { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool IsAutomatic { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public int? MatchesToAdd { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool InvertTeams { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool DuplicationIsEnabled { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public MatchdayViewModel? DuplicationStart { get; set; }

        public ICommand AddToDateCommand { get; }

        public ICommand RemoveFromDateCommand { get; }

        public ICommand RemoveCommand { get; }

        public ICommand GenerateCommand { get; }

        public ICommand RegenerateCommand { get; }

        public ICommand ClearCommand { get; }

        public ICommand CollapseAllCommand { get; }

        public ICommand ExpandAllCommand { get; }

        public ICommand AddMatchesCommand { get; }

        public ICommand InvertTeamsCommand { get; }

        public void Load(IMatchdayParent parent)
        {
            if (parent != Parent)
            {
                Parent = parent;
                var countTeams = parent.GetAvailableTeams().Count();
                var defaultCountMatchdays = Math.Max(1, (countTeams - 1) * 2 - parent.Matchdays.Count);

                _availableMatchdays.Set(parent.Matchdays.OrderBy(x => x.Date));
                NamePattern = MyClubResources.MatchdayNamePattern;
                ShortNamePattern = MyClubResources.MatchdayShortNamePattern;
                StartDisplayDate = parent.SchedulingParameters.StartDate;
                EndDisplayDate = parent.SchedulingParameters.EndDate;
                DefaultTime = parent.SchedulingParameters.StartTime;
                MatchesToAdd = countTeams / 2;
                AutomaticViewModel.Reset(new Period(parent.SchedulingParameters.StartDate, parent.SchedulingParameters.EndDate), defaultCountMatchdays);
                DuplicationIsEnabled = false;
                DuplicationStart = AvailableMatchdays.FirstOrDefault();
                InvertTeams = true;

                RefreshCore();
            }
        }

        protected override bool CanSave() => Matchdays.Count > 0;

        protected override void RefreshCore() => Clear();

        private async Task GenerateAsync(bool clear = false)
            => await ExecuteAsync(() =>
            {
                if (AutomaticViewModel.ValidateProperties())
                {
                    var dates = AutomaticViewModel.ProvideDates().ToList();

                    if (clear)
                        Clear();

                    if (dates.Count == 0)
                        ToasterManager.ShowWarning(MyClubResources.NoDatesGeneratedWarning);

                    dates.ForEach(x => AddToDate(x.Item1, x.Item2));
                }
                else
                {
                    AutomaticViewModel.GetErrors().ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
                }
            }).ConfigureAwait(false);

        private void Clear()
        {
            if (Matchdays.Count > 0)
            {
                Matchdays.Clear();
            }
            ComputeNextIndex();
        }

        private void ComputeNextIndex() => NextIndex = (Parent?.Matchdays.Count ?? 0) + 1;

        private void AddToDate(DateTime date, TimeSpan? time = null)
        {
            Matchdays.Add(CreateEditableMatchday(date, time));
            NextIndex++;
        }

        private EditableMatchdayWrapper CreateEditableMatchday(DateTime date, TimeSpan? time = null)
        {
            var matchday = new EditableMatchdayViewModel(
                new ItemsSourceProvider<MatchdayViewModel>(Parent?.Matchdays.ToList() ?? []),
                _stadiumsProvider,
                new ItemsSourceProvider<TeamViewModel>(Parent?.GetAvailableTeams().ToList() ?? []),
                (Parent?.SchedulingParameters.UseTeamVenues).IsTrue())
            {
                Date = date,
                Time = time ?? DefaultTime,
                Name = StageNamesFactory.ComputePattern(NamePattern.OrEmpty(), NextIndex, date),
                ShortName = StageNamesFactory.ComputePattern(ShortNamePattern.OrEmpty(), NextIndex, date),
            };

            if (DuplicationIsEnabled && DuplicationStart is not null)
            {
                matchday.DuplicateMatchday(DuplicationStart, InvertTeams);
                DuplicationStart = Parent?.Matchdays.OrderBy(x => x.OriginDate).FirstOrDefault(x => x.OriginDate.IsAfter(DuplicationStart.Date));
            }
            return new(matchday);
        }

        private void Remove(EditableMatchdayWrapper? matchday)
        {
            if (matchday is not null)
            {
                Matchdays.Remove(matchday);
                NextIndex--;
            }
        }

        protected override void SaveCore()
        {
            using (_scheduleChangedDeferrer.Defer())
                _matchdayService.Save(Matchdays.Select(x => new MatchdayDto
                {
                    Date = x.Item.Date.GetValueOrDefault().ToUtcDateTime(x.Item.Time.GetValueOrDefault()),
                    Name = x.Item.Name,
                    ShortName = x.Item.ShortName,
                    MatchesToAdd = x.Item.Matches.Where(x => !x.Id.HasValue && x.IsValid()).Select(x => new MatchDto
                    {
                        AwayTeamId = x.AwayTeam!.Id,
                        HomeTeamId = x.HomeTeam!.Id,
                        Date = x.Date!.Value.ToUtcDateTime(x.Time!.Value),
                        Stadium = x.StadiumSelection.SelectedItem is not null ? new StadiumDto
                        {
                            Id = x.StadiumSelection.SelectedItem.Id,
                            Name = x.StadiumSelection.SelectedItem.Name,
                            Ground = x.StadiumSelection.SelectedItem.Ground,
                            Address = x.StadiumSelection.SelectedItem.Address,
                        } : null,
                        State = MatchState.None
                    }).ToList()
                }).ToList());
            Matchdays.Clear();
        }
    }

    internal class EditableMatchdayWrapper : EditableWrapper<EditableMatchdayViewModel>, IAppointment
    {
        public EditableMatchdayWrapper(EditableMatchdayViewModel item) : base(item)
        {
        }

        public bool IsExpanded { get; set; }

        public DateTime StartDate => Item.Date.GetValueOrDefault().BeginningOfDay();

        public DateTime EndDate => Item.Date.GetValueOrDefault().EndOfDay();
    }
}
