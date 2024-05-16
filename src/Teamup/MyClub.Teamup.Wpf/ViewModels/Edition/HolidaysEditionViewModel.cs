// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using DynamicData.Binding;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.HolidaysAggregate;
using MyClub.CrossCutting.Localization;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class HolidaysEditionViewModel : EntityEditionViewModel<Holidays, HolidaysDto, HolidaysService>
    {
        [Display(Name = nameof(Label), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string Label { get; set; } = string.Empty;

        [IsRequired]
        [ValidateProperty(nameof(EndDate))]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public virtual DateTime? StartDate { get; set; }

        [IsRequired]
        [ValidateProperty(nameof(StartDate))]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public virtual DateTime? EndDate { get; set; }

        public HolidaysEditionViewModel(HolidaysService holidaysService)
            : base(holidaysService)
        {
            ValidationRules.AddNotNull<HolidaysEditionViewModel, DateTime?>(x => x.StartDate, MessageResources.FieldStartDateMustBeLowerOrEqualsThanEndDateError, x => x.HasValue && EndDate.HasValue && x.Value.BeginningOfDay() <= EndDate.Value.EndOfDay());
            ValidationRules.AddNotNull<HolidaysEditionViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => StartDate.HasValue && x.HasValue && StartDate.Value.BeginningOfDay() <= x.Value.EndOfDay());

            Disposables.AddRange(
            [
                this.WhenPropertyChanged(x => x.StartDate).Subscribe(_ =>
                {
                    if (StartDate.HasValue && StartDate >= EndDate)
                        EndDate = StartDate.Value.AddDays(1);
                })
            ]);
        }

        protected override HolidaysDto ToDto() => new()
        {
            Id = ItemId,
            Label = Label,
            EndDate = EndDate?.EndOfDay() ?? throw new NullOrEmptyException(nameof(EndDate)),
            StartDate = StartDate?.BeginningOfDay() ?? throw new NullOrEmptyException(nameof(StartDate))
        };

        protected override void RefreshFrom(Holidays item)
        {
            Label = item.Label;
            StartDate = item.Period.Start.Date;
            EndDate = item.Period.End.Date;
        }

        protected override void ResetItem()
        {
            var defaultValues = HolidaysService.NewHolidays(DateTime.Today, DateTime.Today);
            Label = defaultValues.Label ?? string.Empty;
            StartDate = defaultValues.StartDate.Date;
            EndDate = defaultValues.EndDate.Date;
        }
    }
}
