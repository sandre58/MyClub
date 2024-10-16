// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Display;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class MatchdaysEditionParametersManualViewModel : EditableObject, IAddMatchdaysMethodViewModel
    {
        public MatchdaysEditionParametersManualViewModel()
        {
            ClearCommand = CommandsManager.Create(Dates.Clear, () => Dates.Count > 0);
            AddToDateCommand = CommandsManager.CreateNotNull<DateTime>(AddToDate, x => StartDisplayDate.BeginningOfDay().ToPeriod(EndDisplayDate.EndOfDay()).Contains(x));
            RemoveFromDateCommand = CommandsManager.CreateNotNull<DateTime>(x => Remove(Dates.LastOrDefault(y => y.Item == x)), x => Dates.Any(y => y.Item == x));
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public DateOnly StartDisplayDate { get; private set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public DateOnly EndDisplayDate { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public DisplayModeMonth DateSelection { get; } = new();

        public UiObservableCollection<EditableDateWrapper> Dates { get; } = [];

        public ICommand AddToDateCommand { get; }

        public ICommand RemoveFromDateCommand { get; }

        public ICommand ClearCommand { get; }

        public void Reset(IMatchdaysStageViewModel stage)
        {
            StartDisplayDate = stage.ProvideStartDate();
            EndDisplayDate = stage.ProvideEndDate();
            Dates.Clear();
        }

        public AddMatchdaysDatesParametersDto ProvideDatesParameters()
            => new AddMatchdaysManualDatesParametersDto
            {
                Dates = Dates.OrderBy(x => x.Item).Select(x => x.Item).ToList(),
            };

        private void AddToDate(DateTime date) => Dates.Add(new EditableDateWrapper(date));

        private void Remove(EditableDateWrapper? date)
        {
            if (date is not null)
                Dates.Remove(date);
        }
    }

    internal class EditableDateWrapper : EditableWrapper<DateTime>, IAppointment
    {
        public EditableDateWrapper(DateTime date) : base(date) { }

        public DateTime StartDate => Item.BeginningOfDay();

        public DateTime EndDate => Item.EndOfDay();

        public override bool Equals(object? obj) => obj is EditableDateWrapper wrapper && ReferenceEquals(this, wrapper);

        public override int GetHashCode() => Item.GetHashCode();
    }
}
