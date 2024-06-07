// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DocumentFormat.OpenXml.Spreadsheet;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.DynamicData.Extensions;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Units;
using MyNet.Wpf.Controls;
using MyNet.Wpf.DragAndDrop;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulingAssistant
{
    internal class SchedulingAssistantViewModel : EditionViewModel
    {
        private readonly ProjectService _projectService;
        private readonly StadiumsProvider _stadiumsProvider;

        public SchedulingAssistantViewModel(ProjectService projectService, StadiumsProvider stadiumsProvider)
        {
            _projectService = projectService;
            _stadiumsProvider = stadiumsProvider;
            Matches = new SchedulingMatchesListViewModel();
            DropHandler = new((x, y) => x.OfType<EditableSchedulingMatchViewModel>().ForEach(z => z.SetDate(y)),
                               x => x.All(y => y is EditableSchedulingMatchViewModel editableMatch && editableMatch.Item.CanBeRescheduled));

            ShowConflictsCommand = CommandsManager.Create(() => ShowConflicts());
            UpdateSelectionCommand = CommandsManager.Create(() => Update(Matches.SelectedItems?.OfType<CalendarAppointment>() ?? []), () => Matches.SelectedItems is not null && Matches.SelectedItems.OfType<CalendarAppointment>().Any() && CanUpdate());

            Disposables.AddRange(
            [
                DateSelection.WhenPropertyChanged(x => x.DisplayDate, false).Subscribe(_ => DateTimeSelection.DisplayDate = DateSelection.DisplayDate.SetTime(DateTimeSelection.DisplayDate.TimeOfDay)),
                DateTimeSelection.WhenPropertyChanged(x => x.DisplayDate, false).Subscribe(_ => DateSelection.DisplayDate = DateTimeSelection.DisplayDate.BeginningOfDay()),
                Matches.WrappersSource.ToObservableChangeSet().WhenAnyPropertyChanged().Subscribe(_ => ComputeAllConflicts()),
                Matches.WrappersSource.ToObservableChangeSet().MergeManyEx(x => x.Conflicts.ToObservableChangeSet()).Subscribe(_ => RaisePropertyChanged(nameof(HasConflicts))),
                Matches.WhenPropertyChanged(x => x.SelectedItems).Subscribe(_ => {
                    if(Matches.SelectedItems is null || !Matches.SelectedItems.OfType<CalendarAppointment>().Any())
                        ClearSelectionData();
                })
            ]);
        }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public CalendarDropHandler DropHandler { get; }

        [CanBeValidated]
        [CanSetIsModified]
        public SchedulingMatchesListViewModel Matches { get; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public DisplayModeMonth DateSelection { get; } = new();

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public DisplayModeDay DateTimeSelection { get; } = new(2, 8.Hours(), 23.Hours());

        public bool HasConflicts => Matches.WrappersSource.Any(x => x.Conflicts.Any());

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public StadiumViewModel? SelectedStadium { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public int? SelectedDefaultOffsetValue { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public int? SelectedOffsetValue { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public TimeUnit SelectedOffsetUnit { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool SelectedOffsetByDate { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public DateTime? SelectedOffsetDate { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public TimeSpan? SelectedOffsetTime { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<StadiumViewModel> Stadiums => _stadiumsProvider.Items;

        public ICommand ShowConflictsCommand { get; }

        public ICommand UpdateSelectionCommand { get; }

        private bool CanUpdate()
            => SelectedStadium is not null
                   || SelectedDefaultOffsetValue.HasValue && SelectedDefaultOffsetValue < int.MaxValue
                        || SelectedDefaultOffsetValue == int.MaxValue
                        && (SelectedOffsetByDate ? SelectedOffsetDate.HasValue || SelectedOffsetTime.HasValue : SelectedOffsetValue.HasValue);

        private void Update(IEnumerable<CalendarAppointment> appointments)
        {
            appointments.ToList().ForEach(x =>
            {
                var match = x.DataContext as EditableSchedulingMatchViewModel;

                if (match is not null)
                {
                    if (SelectedStadium is not null)
                        match.Stadium = SelectedStadium;

                    if (SelectedDefaultOffsetValue.HasValue)
                    {
                        if (SelectedDefaultOffsetValue < int.MaxValue)
                            match.SetDate(match.StartDate.AddMinutes(SelectedDefaultOffsetValue.Value));
                        else
                        {
                            if (SelectedOffsetByDate)
                            {
                                var date = match.StartDate.Date;
                                var time = match.StartDate.TimeOfDay;

                                if (SelectedOffsetDate.HasValue)
                                    date = SelectedOffsetDate.Value.Date;

                                if (SelectedOffsetTime.HasValue)
                                    time = SelectedOffsetTime.Value;

                                match.SetDate(date.ToLocalDateTime(time));
                            }
                            else if (SelectedOffsetValue.HasValue)
                                match.SetDate(match.StartDate.Add(SelectedOffsetValue.Value, SelectedOffsetUnit));
                        }
                    }
                }
            });
            ClearSelectionData();
        }

        private void ClearSelectionData()
        {
            SelectedStadium = null;
            SelectedDefaultOffsetValue = null;
            SelectedOffsetValue = null;
            SelectedOffsetUnit = TimeUnit.Minute;
            SelectedOffsetByDate = false;
            SelectedOffsetDate = null;
            SelectedOffsetTime = null;
        }

        private void ComputeAllConflicts()
        {
            var result = new Dictionary<EditableSchedulingMatchViewModel, IEnumerable<SchedulingConflict>>();
            var associations = Matches.WrappersSource.RoundRobin().SelectMany(x => x).ToList();
            foreach (var associationsGroupped in associations.GroupBy(x => x.item1))
            {
                foreach (var (match1, match2) in associationsGroupped)
                {
                    var conflicts = GetConflictsBetween(match1, match2);

                    result.AddOrUpdate(match1, result.GetOrDefault(match1, new List<SchedulingConflict>())!.Union(conflicts.Item1));
                    result.AddOrUpdate(match2, result.GetOrDefault(match2, new List<SchedulingConflict>())!.Union(conflicts.Item2));
                }
            }

            foreach (var (match, conflicts) in result)
                match.SetConflicts(conflicts);
        }

        private (IEnumerable<SchedulingConflict>, IEnumerable<SchedulingConflict>) GetConflictsBetween(EditableSchedulingMatchViewModel match1, EditableSchedulingMatchViewModel match2)
        {
            var parameters = _projectService.GetParameters();
            var period1 = new Period(match1.StartDate, match1.EndDate);
            var period2 = new Period(match2.StartDate, match2.EndDate);

            var conflicts1 = new List<SchedulingConflict>();
            var conflicts2 = new List<SchedulingConflict>();

            if (match2.Item.Participate(match1.Item.HomeTeam) || match2.Item.Participate(match1.Item.AwayTeam))
            {
                if (period1.Intersect(period2))
                {
                    if (match2.Item.Participate(match1.Item.HomeTeam))
                        conflicts1.Add(new SchedulingTeamBusyConflict(match1.Item.HomeTeam));
                    if (match1.Item.Participate(match2.Item.HomeTeam))
                        conflicts2.Add(new SchedulingTeamBusyConflict(match2.Item.HomeTeam));
                    if (match2.Item.Participate(match1.Item.AwayTeam))
                        conflicts1.Add(new SchedulingTeamBusyConflict(match1.Item.AwayTeam));
                    if (match1.Item.Participate(match2.Item.AwayTeam))
                        conflicts2.Add(new SchedulingTeamBusyConflict(match2.Item.AwayTeam));
                }
                else
                {
                    var periodWithRestTime1 = new Period(period1.Start, period1.End + parameters.MinimumRestTime);
                    var periodWithRestTime2 = new Period(period2.Start, period2.End + parameters.MinimumRestTime);

                    if (periodWithRestTime1.Intersect(periodWithRestTime2))
                    {
                        if (match2.Item.Participate(match1.Item.HomeTeam))
                            conflicts1.Add(new SchedulingTeamRestTimeNotRespectedConflict(match1.Item.HomeTeam));
                        if (match1.Item.Participate(match2.Item.HomeTeam))
                            conflicts2.Add(new SchedulingTeamRestTimeNotRespectedConflict(match2.Item.HomeTeam));
                        if (match2.Item.Participate(match1.Item.AwayTeam))
                            conflicts1.Add(new SchedulingTeamRestTimeNotRespectedConflict(match1.Item.AwayTeam));
                        if (match1.Item.Participate(match2.Item.AwayTeam))
                            conflicts2.Add(new SchedulingTeamRestTimeNotRespectedConflict(match2.Item.AwayTeam));
                    }
                }
            }

            if (match1.Stadium is not null && match1.Stadium.Equals(match2.Stadium))
            {
                if (period1.Intersect(period2))
                {
                    conflicts1.Add(new SchedulingStadiumOccupancyConflict(match1.Stadium));
                    conflicts2.Add(new SchedulingStadiumOccupancyConflict(match2.Stadium));
                }
                else
                {
                    var periodWithRotationTime1 = new Period(period1.Start, period1.End + parameters.RotationTime);
                    var periodWithRotationTime2 = new Period(period2.Start, period2.End + parameters.RotationTime);

                    if (periodWithRotationTime1.Intersect(periodWithRotationTime2))
                    {
                        conflicts1.Add(new SchedulingRotationTimeNotRespectedConflict(match1.Stadium));
                        conflicts2.Add(new SchedulingRotationTimeNotRespectedConflict(match2.Stadium));
                    }
                }
            }

            return (conflicts1, conflicts2);
        }

        private void ShowConflicts()
        {
            var matchWithConflicts = Matches.WrappersSource.FirstOrDefault(x => x.Conflicts.Any());

            if (matchWithConflicts is not null)
            {
                Matches.Filters.Clear();
                DateTimeSelection.DisplayDate = matchWithConflicts.StartDate;
            }
        }

        public void Load(IEnumerable<MatchViewModel> matches, DateTime? displayDate = null)
        {
            if (displayDate.HasValue)
                DateTimeSelection.DisplayDate = displayDate.Value;

            Matches.Load(matches);
            ComputeAllConflicts();
        }

        protected override void ResetCore() => Matches.WrappersSource.ForEach(x => x.Reset());

        protected override void RefreshCore() => ResetCore();

        protected override void SaveCore() { }

        public override bool IsModified() => Matches.WrappersSource.Any(x => x.IsModified());
    }
}
