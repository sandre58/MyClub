// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class MatchdaysEditionResultViewModel : EditableObject
    {
        public MatchdaysEditionResultViewModel()
        {
            RemoveCommand = CommandsManager.CreateNotNull<EditableMatchdayWrapper>(Remove);
            ClearCommand = CommandsManager.Create(Matchdays.Clear, () => Matchdays.Count > 0);
            CollapseAllCommand = CommandsManager.Create(() => Matchdays.ForEach(x => x.IsExpanded = false), () => Matchdays.Any(x => x.IsExpanded));
            ExpandAllCommand = CommandsManager.Create(() => Matchdays.ForEach(x => x.IsExpanded = true), () => Matchdays.Any(x => !x.IsExpanded));
            InvertTeamsCommand = CommandsManager.Create(() => Matchdays.ForEach(x => x.Item.InvertTeams()), () => Matchdays.Any(x => x.Item.Matches.Count > 0));
            AddMatchesCommand = CommandsManager.Create(() => Matchdays.ForEach(x => EnumerableHelper.Iteration(MatchesToAdd!.Value, _ => x.Item.AddMatch())), () => Matchdays.Count > 0 && MatchesToAdd > 0);
        }

        public UiObservableCollection<EditableMatchdayWrapper> Matchdays { get; } = [];

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public int? MatchesToAdd { get; set; }

        public ICommand RemoveCommand { get; }

        public ICommand ClearCommand { get; }

        public ICommand CollapseAllCommand { get; }

        public ICommand ExpandAllCommand { get; }

        public ICommand AddMatchesCommand { get; }

        public ICommand InvertTeamsCommand { get; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool ScheduleAutomatic { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool ScheduleStadiumsAutomatic { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanScheduleAutomatic { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanScheduleStadiumsAutomatic { get; set; }

        public MatchdaysDto ToResultDto(Guid? stageId) => new()
        {
            ScheduleAutomatic = ScheduleAutomatic,
            ScheduleStadiumsAutomatic = ScheduleStadiumsAutomatic,
            StartDate = Matchdays.MinOrDefault(x => x.Item.CurrentDate.ToUtcOrDefault()),
            Matchdays = Matchdays.Select(x => new MatchdayDto
            {
                StageId = stageId,
                Date = x.Item.CurrentDate.ToUtcOrDefault(),
                Name = x.Item.Name,
                ShortName = x.Item.ShortName,
                MatchesToAdd = x.Item.Matches.Where(x => !x.Id.HasValue && x.IsValid()).Select(x => new MatchDto
                {
                    AwayTeamId = x.AwayTeam!.Id,
                    HomeTeamId = x.HomeTeam!.Id,
                    Date = x.CurrentDate.ToUtcOrDefault(),
                    Stadium = x.StadiumSelection.SelectedItem is not null ? new StadiumDto
                    {
                        Id = x.StadiumSelection.SelectedItem.Id,
                        Name = x.StadiumSelection.SelectedItem.Name,
                        Ground = x.StadiumSelection.SelectedItem.Ground,
                        Address = x.StadiumSelection.SelectedItem.Address,
                    } : null,
                    State = MatchState.None
                }).ToList()
            }).ToList()
        };

        public void Reset(LeagueViewModel stage)
        {
            Matchdays.Clear();
            MatchesToAdd = 1;
            CanScheduleAutomatic = stage.CanAutomaticReschedule();
            CanScheduleStadiumsAutomatic = stage.CanAutomaticRescheduleVenue();
            ScheduleAutomatic = false;
            ScheduleStadiumsAutomatic = false;
        }

        public void Refresh(LeagueViewModel stage)
        {
            CanScheduleAutomatic = stage.CanAutomaticReschedule();
            CanScheduleStadiumsAutomatic = stage.CanAutomaticRescheduleVenue();
            if (!CanScheduleAutomatic) ScheduleAutomatic = false;
            if (!CanScheduleStadiumsAutomatic) ScheduleStadiumsAutomatic = false;
        }

        public void Update(IEnumerable<EditableMatchdayWrapper> matchdays) => Matchdays.Set(matchdays);

        private void Remove(EditableMatchdayWrapper? matchday)
        {
            if (matchday is not null)
                Matchdays.Remove(matchday);
        }
    }

    internal class EditableMatchdayWrapper : EditableWrapper<EditableMatchdayViewModel>
    {
        public EditableMatchdayWrapper(EditableMatchdayViewModel item) : base(item)
        {
        }

        public bool IsExpanded { get; set; }
    }
}
