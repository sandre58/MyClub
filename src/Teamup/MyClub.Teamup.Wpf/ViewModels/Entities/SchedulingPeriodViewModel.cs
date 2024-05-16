// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Media;
using DynamicData.Binding;
using MyClub.Domain;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{

    internal abstract class SchedulingPeriodViewModel<T> : EntityViewModelBase<T>, ISchedulingPeriodViewModel
        where T : Entity, IHasPeriod
    {
        protected SchedulingPeriodViewModel(T item) : base(item)
            => Disposables.AddRange(
            [
                Item.Period.WhenPropertyChanged(x => x.Start).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    UpdateWeeks();
                }),
                Item.Period.WhenPropertyChanged(x => x.End).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(EndDate));
                    UpdateWeeks();
                })
            ]);

        public DateTime StartDate => Item.Period.Start.Date;

        public DateTime EndDate => Item.Period.End.Date;

        public int Weeks { get; private set; }

        public abstract Color Color { get; }

        public abstract string Label { get; }

        public bool ContainsDate(DateTime date) => Item.Period.Contains(date);

        public void UpdateWeeks() => Weeks = StartDate.NumberOfWeeks(EndDate);
    }
}
