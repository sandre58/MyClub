// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using DynamicData.Binding;
using MyNet.UI.ViewModels.Edition;
using MyNet.Wpf.Extensions;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Generator;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.CycleAggregate;
using MyClub.Teamup.Wpf.Services.Providers;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class CycleEditionViewModel : EntityEditionViewModel<Cycle, CycleDto, CycleService>
    {
        [Display(Name = nameof(Label), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string Label { get; set; } = string.Empty;

        [IsRequired]
        [ValidateProperty(nameof(EndDate))]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        [IsRequired]
        [ValidateProperty(nameof(StartDate))]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateTime? EndDate { get; set; }

        public Color Color { get; set; }

        public StringListEditionViewModel TechnicalGoalsViewModel { get; } = new(MyClubResources.TechnicalGoals);

        public StringListEditionViewModel TacticalGoalsViewModel { get; } = new(MyClubResources.TacticalGoals);

        public StringListEditionViewModel PhysicalGoalsViewModel { get; } = new(MyClubResources.PhysicalGoals);

        public StringListEditionViewModel MentalGoalsViewModel { get; } = new(MyClubResources.MentalGoals);

        public CycleEditionViewModel(CyclesProvider cyclesProvider, CycleService cycleService)
            : base(cycleService)
        {
            ValidationRules.AddNotNull<CycleEditionViewModel, DateTime?>(x => x.StartDate, MessageResources.FieldStartDateMustBeLowerOrEqualsThanEndDateError, x => x.HasValue && EndDate.HasValue && x.Value.BeginningOfDay() <= EndDate.Value.EndOfDay());
            ValidationRules.AddNotNull<CycleEditionViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => StartDate.HasValue && x.HasValue && StartDate.Value.BeginningOfDay() <= x.Value.EndOfDay());
            ValidationRules.AddNotNull<CycleEditionViewModel, DateTime?>(x => x.StartDate, MyClubResources.CycleDatesIsBetweenOtherCyclesError, x => !x.HasValue || cyclesProvider.Items.All(y => y.Id == ItemId || y.Id != ItemId && !y.ContainsDate(x.Value)));
            ValidationRules.AddNotNull<CycleEditionViewModel, DateTime?>(x => x.EndDate, MyClubResources.CycleDatesIsBetweenOtherCyclesError, x => !x.HasValue || cyclesProvider.Items.All(y => y.Id == ItemId || y.Id != ItemId && !y.ContainsDate(x.Value)));

            Disposables.AddRange(
            [
                this.WhenPropertyChanged(x => x.StartDate).Subscribe(_ =>
                {
                    if (StartDate.HasValue && StartDate >= EndDate)
                        EndDate = StartDate.Value.AddDays(1);
                })
            ]);
        }

        protected override CycleDto ToDto() => new()
        {
            Id = ItemId,
            Label = Label,
            EndDate = EndDate?.EndOfDay() ?? throw new NullOrEmptyException(nameof(EndDate)),
            StartDate = StartDate?.BeginningOfDay() ?? throw new NullOrEmptyException(nameof(StartDate)),
            Color = Color.ToString(),
            MentalGoals = MentalGoalsViewModel.Items.Select(x => x.Value).NotNull().ToList(),
            PhysicalGoals = PhysicalGoalsViewModel.Items.Select(x => x.Value).NotNull().ToList(),
            TacticalGoals = TacticalGoalsViewModel.Items.Select(x => x.Value).NotNull().ToList(),
            TechnicalGoals = TechnicalGoalsViewModel.Items.Select(x => x.Value).NotNull().ToList()
        };

        protected override void RefreshFrom(Cycle item)
        {
            Label = item.Label;
            StartDate = item.Period.Start.Date;
            EndDate = item.Period.End.Date;
            Color = item.Color.ToColor() ?? default;
            MentalGoalsViewModel.SetSource(item.MentalGoals.ToObservableCollection());
            PhysicalGoalsViewModel.SetSource(item.PhysicalGoals.ToObservableCollection());
            TacticalGoalsViewModel.SetSource(item.TacticalGoals.ToObservableCollection());
            TechnicalGoalsViewModel.SetSource(item.TechnicalGoals.ToObservableCollection());
        }

        protected override void ResetItem()
        {
            var defaultValues = CycleService.NewCycle(DateTime.Today, DateTime.Today);
            Label = defaultValues.Label ?? string.Empty;
            StartDate = defaultValues.StartDate.Date;
            EndDate = defaultValues.EndDate.Date;
            Color = RandomGenerator.Color().ToColor() ?? default;
            MentalGoalsViewModel.SetSource(defaultValues.MentalGoals?.ToObservableCollection() ?? []);
            PhysicalGoalsViewModel.SetSource(defaultValues.PhysicalGoals?.ToObservableCollection() ?? []);
            TacticalGoalsViewModel.SetSource(defaultValues.TacticalGoals?.ToObservableCollection() ?? []);
            TechnicalGoalsViewModel.SetSource(defaultValues.TechnicalGoals?.ToObservableCollection() ?? []);
        }
    }
}
