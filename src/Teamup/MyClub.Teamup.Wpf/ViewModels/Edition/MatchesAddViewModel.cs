// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.DynamicData.Extensions;
using MyNet.Observable.Attributes;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Wpf.Extensions;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class MatchesAddViewModel : EditionViewModel
    {
        private readonly MatchService _matchService;
        private readonly StadiumPresentationService _stadiumPresentationService;
        private readonly TeamPresentationService _teamPresentationService;
        private readonly StadiumsProvider _stadiumsProvider;

        public ObservableCollection<EditableMatchViewModel> Matches { get; private set; } = [];

        public MatchesAddViewModel(MatchService matchService,
                                   StadiumsProvider stadiumsProvider,
                                   StadiumPresentationService stadiumPresentationService,
                                   TeamPresentationService teamPresentationService,
                                   IMatchParent parent)
        {
            _matchService = matchService;
            _stadiumsProvider = stadiumsProvider;
            _stadiumPresentationService = stadiumPresentationService;
            _teamPresentationService = teamPresentationService;
            Parent = parent;
            Competition = parent.GetCompetition();
            Mode = ScreenMode.Creation;

            RemoveCommand = CommandsManager.CreateNotNull<EditableMatchViewModel>(x => Matches.Remove(x));
            RemoveSelectedItemsCommand = CommandsManager.Create(() => Matches.RemoveMany(SelectedRows?.Cast<EditableMatchViewModel>() ?? []), () => SelectedRows is not null && SelectedRows.OfType<EditableMatchViewModel>().Any());
            EditDateSelectedItemsCommand = CommandsManager.Create(() => SetValueInSelectedRows(y => y.Date = EditDate?.Date), () => EditDate is not null && SelectedRows is not null && SelectedRows.OfType<EditableMatchViewModel>().Any());
            EditTimeSelectedItemsCommand = CommandsManager.Create(() => SetValueInSelectedRows(y => y.Time = EditTime!.Value), () => EditTime is not null && SelectedRows is not null && SelectedRows.OfType<EditableMatchViewModel>().Any());
            AddCommand = CommandsManager.Create(() => Matches.Add(CreateEditableMatch()));

            Matches.Add(CreateEditableMatch());
            UpdateTitle();
        }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public string? SubTitle => Equals(Parent, Competition) ? null : Parent?.Name;

        public IMatchParent Parent { get; }

        public CompetitionViewModel Competition { get; private set; }

        public IEnumerable? SelectedRows { get; set; }

        public DateTime? EditDate { get; set; }

        public TimeSpan? EditTime { get; set; }

        public ICommand RemoveCommand { get; }

        public ICommand RemoveSelectedItemsCommand { get; }

        public ICommand EditDateSelectedItemsCommand { get; }

        public ICommand EditTimeSelectedItemsCommand { get; }

        public ICommand AddCommand { get; }

        protected override string CreateTitle()
        {
            RaisePropertyChanged(nameof(SubTitle));
            return Competition?.Name ?? string.Empty;
        }

        private EditableMatchViewModel CreateEditableMatch()
        {
            var result = new EditableMatchViewModel(_stadiumsProvider, _stadiumPresentationService, _teamPresentationService);

            result.HomeTeamSelection.UpdateSource(Parent.GetAvailableTeams());
            result.AwayTeamSelection.UpdateSource(Parent.GetAvailableTeams());

            var datetime = Parent.GetDefaultDateTime();
            result.Date = datetime.Date;
            result.Time = datetime.TimeOfDay;

            return result;
        }

        protected virtual void SetValueInSelectedRows(Action<EditableMatchViewModel> action) => SelectedRows?.Cast<EditableMatchViewModel>().ForEach(action);

        #region Validate

        protected override void SaveCore()
            => _ = _matchService.Save(Matches.Where(x => x.HomeTeamSelection.SelectedItem is not null && x.AwayTeamSelection.SelectedItem is not null)
                                         .Select(x => new MatchDto
                                         {
                                             ParentId = Parent.Id,
                                             HomeTeamId = x.HomeTeamSelection.SelectedItem!.Id,
                                             AwayTeamId = x.AwayTeamSelection.SelectedItem!.Id,
                                             Date = x.Date.GetValueOrDefault().ToUtcDateTime(x.Time),
                                             Stadium = x.StadiumSelection.SelectedItem is not null ? new StadiumDto
                                             {
                                                 Id = x.StadiumSelection.SelectedItem.Id,
                                                 Name = x.StadiumSelection.SelectedItem.Name,
                                                 Ground = x.StadiumSelection.SelectedItem.Ground,
                                                 Address = x.StadiumSelection.SelectedItem?.Address,
                                             } : null,
                                         }).ToList(), false).Count;

        #endregion

        protected override void Cleanup()
        {
            base.Cleanup();
            Matches.ForEach(x => x.Dispose());
        }
    }
}
