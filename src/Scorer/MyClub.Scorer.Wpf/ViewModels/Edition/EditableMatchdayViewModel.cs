// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableMatchdayViewModel : EditableObject
    {
        private readonly bool _useHomeTeamStadium;
        private readonly ISourceProvider<IStadiumViewModel> _stadiums;
        private readonly ISourceProvider<ITeamViewModel> _teams;

        public EditableMatchdayViewModel(ISourceProvider<MatchdayViewModel> matchdays,
                                         ISourceProvider<IStadiumViewModel> stadiums,
                                         ISourceProvider<ITeamViewModel> teams,
                                         bool useHomeTeamStadium = false)
        {
            _useHomeTeamStadium = useHomeTeamStadium;
            _stadiums = stadiums;
            _teams = teams;
            Matchdays = matchdays.Source;

            InvertTeamsCommand = CommandsManager.Create(InvertTeams, () => Matches.Count > 0);
            AddMatchCommand = CommandsManager.Create(AddMatch, () => _teams.Source.Count > 0);
            RemoveMatchCommand = CommandsManager.CreateNotNull<EditableMatchViewModel>(RemoveMatch);
            ClearMatchesCommand = CommandsManager.Create(ClearMatches, () => Matches.Count > 0);
            ClearDuplicatedMatchdayCommand = CommandsManager.Create(ClearDuplicatedMatchday);
            DuplicateMatchdayCommand = CommandsManager.CreateNotNull<MatchdayViewModel>(DuplicateMatchday);
        }
        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public Guid? Id { get; }

        public EditableDateTime CurrentDate { get; set; } = new();

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string? Name { get; set; }

        [IsRequired]
        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        public string? ShortName { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public MatchdayViewModel? DuplicatedMatchday { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<MatchdayViewModel> Matchdays { get; }

        public ObservableCollection<EditableMatchViewModel> Matches { get; } = [];

        public ICommand AddMatchCommand { get; }

        public ICommand RemoveMatchCommand { get; }

        public ICommand InvertTeamsCommand { get; }

        public ICommand ClearMatchesCommand { get; }

        public ICommand DuplicateMatchdayCommand { get; }

        public ICommand ClearDuplicatedMatchdayCommand { get; }

        private void RemoveMatch(EditableMatchViewModel item) => Matches.Remove(item);

        public void AddMatch()
        {
            var item = new EditableMatchViewModel(_teams, _stadiums, _useHomeTeamStadium);

            item.CurrentDate.Date = CurrentDate.Date;
            item.CurrentDate.Time = CurrentDate.Time;

            Matches.Add(item);
        }

        public void InvertTeams() => Matches.ForEach(x => x.InvertTeams());

        private void DuplicateMatchday(MatchdayViewModel matchday)
        {
            DuplicatedMatchday = matchday;
            Matches.Set(matchday.Matches.OrderBy(x => x.Date).Select(x =>
            {
                var result = new EditableMatchViewModel(_teams, _stadiums, _useHomeTeamStadium);

                result.CurrentDate.Date = CurrentDate.Date;
                result.CurrentDate.Time = CurrentDate.Time;

                if (result.CurrentDate.HasValue)
                    result.CurrentDate.Load(CurrentDate.DateTime.GetValueOrDefault());
                result.HomeTeam = result.AvailableTeams.GetById(x.HomeTeam.Id);
                result.AwayTeam = result.AvailableTeams.GetById(x.AwayTeam.Id);
                result.StadiumSelection.Select(x.Stadium?.Id);

                return result;
            }));
        }

        private void ClearDuplicatedMatchday()
        {
            DuplicatedMatchday = null;
            Matches.Clear();
        }

        private void ClearMatches() => Matches.Clear();
    }
}
