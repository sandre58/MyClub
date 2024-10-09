// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services.Managers;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class MatchdaysEditionViewModel : EditionViewModel
    {
        private readonly StadiumsProvider _stadiumsProvider;
        private readonly MatchdayService _matchdayService;
        private readonly ConflictsManager _conflictsManager;
        private readonly CompetitionInfoProvider _competitionInfoProvider;
        private readonly MatchdaysEditionResultViewModel _resultViewModel = new();
        private readonly MatchdaysEditionParametersViewModel _parametersViewModel = new();

        public MatchdaysEditionViewModel(CompetitionInfoProvider competitionInfoProvider,
                                         StadiumsProvider stadiumsProvider,
                                         MatchdayService matchdayService,
                                         ConflictsManager conflictsManager)
        {
            _stadiumsProvider = stadiumsProvider;
            _matchdayService = matchdayService;
            _conflictsManager = conflictsManager;
            _competitionInfoProvider = competitionInfoProvider;

            GenerateCommand = CommandsManager.Create(async () => await GenerateAsync().ConfigureAwait(false), () => CurrentViewModel is MatchdaysEditionParametersViewModel);
            ShowParametersCommand = CommandsManager.Create(SwitchView, () => CurrentViewModel is MatchdaysEditionResultViewModel);
            ShowResultsCommand = CommandsManager.Create(SwitchView, () => CurrentViewModel is MatchdaysEditionParametersViewModel && CountMatchdays > 0);

            Disposables.Add(_resultViewModel.Matchdays.ToObservableChangeSet().Subscribe(_ => CountMatchdays = _resultViewModel.Matchdays.Count));

            competitionInfoProvider.LoadRunner.RegisterOnEnd(this, _ => Reset());
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public EditableObject? CurrentViewModel { get; private set; }

        public int CountMatchdays { get; private set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public LeagueViewModel? Stage { get; private set; }

        public ICommand GenerateCommand { get; }

        public ICommand ShowParametersCommand { get; }

        public ICommand ShowResultsCommand { get; }

        private void SwitchView() => CurrentViewModel = CurrentViewModel is MatchdaysEditionParametersViewModel ? _resultViewModel : _parametersViewModel;

        private EditableMatchdayWrapper CreateEditableMatchday(MatchdayDto matchdayDto)
        {
            var teamsProvider = new ItemsSourceProvider<IVirtualTeamViewModel>(Stage?.GetAvailableTeams().ToList() ?? []);
            var matchday = new EditableMatchdayViewModel(
                new ItemsSourceProvider<MatchdayViewModel>(Stage?.Matchdays.ToList() ?? []),
                _stadiumsProvider,
                teamsProvider,
                (Stage?.SchedulingParameters.UseHomeVenue).IsTrue())
            {
                Name = matchdayDto.Name,
                ShortName = matchdayDto.ShortName
            };
            matchday.CurrentDate.Load(matchdayDto.Date);
            matchday.Matches.AddRange(matchdayDto.MatchesToAdd?.Select(x =>
            {
                var match = new EditableMatchViewModel(teamsProvider, _stadiumsProvider, (Stage?.SchedulingParameters.UseHomeVenue).IsTrue())
                {
                    HomeTeam = teamsProvider.Source.GetById(x.HomeTeamId),
                    AwayTeam = teamsProvider.Source.GetById(x.AwayTeamId),
                };
                matchday.CurrentDate.Load(x.Date);
                match.StadiumSelection.Select(x.Stadium?.Id);

                return match;
            }).ToList());

            return new(matchday);
        }

        public void Load(LeagueViewModel stage)
        {
            if (!ReferenceEquals(stage, Stage))
            {
                Stage = stage;

                Reset();
            }
        }

        protected override void RefreshCore()
        {
            if (Stage is not null)
            {
                _parametersViewModel.Refresh(Stage);
                _resultViewModel.Refresh(Stage);
            }
        }

        protected override void ResetCore()
        {
            CurrentViewModel = _parametersViewModel;

            if (Stage is not null)
            {
                _parametersViewModel.Reset(Stage);
                _resultViewModel.Reset(Stage);
            }
        }

        public override bool ValidateProperties() => _resultViewModel.ValidateProperties();

        protected override bool CanSave() => CurrentViewModel is MatchdaysEditionResultViewModel && _resultViewModel.Matchdays.Count > 0;

        protected override void SaveCore()
        {
            using (_conflictsManager.Defer())
            using (_competitionInfoProvider.GetLeague().DeferRefreshRankings())
            {
                var count = _matchdayService.Save(_resultViewModel.ToResultDto(Stage?.Id));
                _parametersViewModel.Index += count;
            }

            SwitchView();
            _resultViewModel.Matchdays.Clear();
        }

        private async Task GenerateAsync()
        => await ExecuteAsync(() =>
        {
            if (_parametersViewModel.ValidateProperties())
            {
                var matchdays = _matchdayService.New(_parametersViewModel.ToParametersDto(Stage?.Id));

                if (matchdays.Count == 0)
                    ToasterManager.ShowError(MyClubResources.NoDatesGeneratedWarning);
                else
                {
                    _resultViewModel.Update(matchdays.Select(CreateEditableMatchday).ToList());
                    SwitchView();
                }
            }
            else
            {
                _parametersViewModel.GetErrors().ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
            }
        }).ConfigureAwait(false);

        protected override Task<bool> CanCancelAsync() => Task.FromResult(true);

        protected override Task<MessageBoxResult> SavingRequestAsync() => Task.FromResult(MessageBoxResult.No);

        protected override void Cleanup()
        {
            _competitionInfoProvider.LoadRunner.Unregister(this);
            base.Cleanup();
        }
    }
}
