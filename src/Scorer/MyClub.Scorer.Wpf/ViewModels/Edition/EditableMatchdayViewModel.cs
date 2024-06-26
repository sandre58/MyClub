﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.ViewModels.Entities;
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
        private readonly ISourceProvider<StadiumViewModel> _stadiums;
        private readonly ISourceProvider<TeamViewModel> _teams;

        public EditableMatchdayViewModel(ISourceProvider<MatchdayViewModel> matchdays,
                                         ISourceProvider<StadiumViewModel> stadiums,
                                         ISourceProvider<TeamViewModel> teams,
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
        }
        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public Guid? Id { get; }

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateTime? Date { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeSpan? Time { get; set; }

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

        private void RemoveMatch(EditableMatchViewModel item) => Matches.Remove(item);

        public void AddMatch() => Matches.Add(new EditableMatchViewModel(_teams, _stadiums, _useHomeTeamStadium)
        {
            Date = Date,
            Time = Time,
        });

        public void InvertTeams() => Matches.ForEach(x => x.InvertTeams());

        public void DuplicateMatchday(MatchdayViewModel matchday, bool invertTeams)
        {
            using (PropertyChangedSuspender.Suspend())
                DuplicatedMatchday = matchday;
            Matches.Set(matchday.Matches.OrderBy(x => x.Date).Select(x =>
            {
                var result = new EditableMatchViewModel(_teams, _stadiums, _useHomeTeamStadium)
                {
                    Date = Date,
                    Time = Time,
                };
                result.HomeTeam = result.AvailableTeams.GetById(x.HomeTeam.Id);
                result.AwayTeam = result.AvailableTeams.GetById(x.AwayTeam.Id);
                result.StadiumSelection.Select(x.Stadium?.Id);

                if (invertTeams)
                    result.InvertTeams();

                return result;
            }));
        }

        private void ClearMatches() => Matches.Clear();

        protected void OnDuplicatedMatchdayChanged()
        {
            if (DuplicatedMatchday is null)
                ClearMatches();
            else
                DuplicateMatchday(DuplicatedMatchday, false);
        }
    }
}
