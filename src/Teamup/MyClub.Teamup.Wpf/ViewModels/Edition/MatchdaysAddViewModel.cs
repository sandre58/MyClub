// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Observable.Attributes;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Extensions;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class MatchdaysAddViewModel : EditionViewModel
    {
        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public string? SubTitle => Equals(Parent, Competition) ? null : Parent?.Name;

        public ObservableCollection<EditableMatchdayViewModel> Matchdays { get; private set; } = [];

        public MatchdaysAddViewModel(IMatchdayParent parent)
        {
            Parent = parent;
            Competition = parent.GetCompetition();
            Mode = ScreenMode.Creation;

            RemoveCommand = CommandsManager.CreateNotNull<EditableMatchdayViewModel>(x => Matchdays.Remove(x));
            AddCommand = CommandsManager.Create(() => Matchdays.Add(CreateMatchdayEditable(ComputeNextDate())));

            UpdateTitle();
        }

        public IMatchdayParent Parent { get; }

        public CompetitionViewModel Competition { get; private set; }

        public ICommand RemoveCommand { get; }

        public ICommand AddCommand { get; }

        protected override string CreateTitle()
        {
            RaisePropertyChanged(nameof(SubTitle));
            return Competition?.Name ?? string.Empty;
        }

        public void Load(IEnumerable<IMatchdayViewModel> matchdays, IEnumerable<DateTime> dates)
        {
            var datesList = dates.ToList();
            var matchdaysList = matchdays.ToList();

            var date = DateTime.Today;
            for (var i = 0; i < Math.Max(matchdaysList.Count, datesList.Count); i++)
            {
                date = datesList.Count > i ? datesList[i] : date.AddDays(7);
                var matchday = matchdaysList.Count > i ? matchdaysList[i] : null;
                Matchdays.Add(CreateMatchdayEditable(date, matchday));
            }
            UpdateTitle();
        }

        private EditableMatchdayViewModel CreateMatchdayEditable(DateTime date, IMatchdayViewModel? matchday = null)
        {
            var name = MyClubResources.Matchday.Increment(Parent.GetAllMatchdays().Select(x => x.Name).Union(Matchdays.Select(x => x.Name)), format: " #");
            var shortName = MyClubResources.Matchday.Substring(0, 1).Increment(Parent.GetAllMatchdays().Select(x => x.ShortName).Union(Matchdays.Select(x => x.ShortName)));
            var result = new EditableMatchdayViewModel
            {
                DuplicatedMatchday = matchday,
                Date = date.Date,
                Time = Parent.GetDefaultDateTime().TimeOfDay,
                Name = name,
                ShortName = shortName,
            };
            return result;
        }

        private DateTime ComputeNextDate()
        {
            var maxDate = Matchdays.Where(x => x.Date.HasValue).MaxOrDefault(x => x.Date!.Value);

            return maxDate == default ? DateTime.Today : maxDate.AddDays(7);
        }

        protected override void SaveCore() { }
    }
}
