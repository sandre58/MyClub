// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DynamicData.Binding;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Domain.Enums;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class AbsenceEditionViewModel : EditionViewModel
    {
        private readonly AbsenceService _playerAbsenceService;
        private DateTime? _defaultStartDate;
        private DateTime? _defaultEndDate;
        private AbsenceType _type;

        public IReadOnlyCollection<PlayerViewModel> Players { get; private set; } = [];

        public Guid? AbsenceId { get; private set; }

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

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public DateTime? CreatedAt { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public string? CreatedBy { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public DateTime? ModifiedAt { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public string? ModifiedBy { get; private set; }

        public AbsenceEditionViewModel(AbsenceService playerAbsenceService)
        {
            _playerAbsenceService = playerAbsenceService;

            ValidationRules.AddNotNull<AbsenceEditionViewModel, DateTime?>(x => x.StartDate, MessageResources.FieldStartDateMustBeLowerOrEqualsThanEndDateError, x => x.HasValue && EndDate.HasValue && x.Value.BeginningOfDay() <= EndDate.Value.EndOfDay());
            ValidationRules.AddNotNull<AbsenceEditionViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => StartDate.HasValue && x.HasValue && StartDate.Value.BeginningOfDay() <= x.Value.EndOfDay());

            Disposables.AddRange(
            [
                this.WhenPropertyChanged(x => x.StartDate).Subscribe(_ =>
                {
                    if (StartDate.HasValue && StartDate >= EndDate)
                        EndDate = StartDate.Value.AddDays(1);
                })
            ]);
            _playerAbsenceService = playerAbsenceService;
        }

        public void Load(IEnumerable<PlayerViewModel> players, Guid? absenceId, AbsenceType type, DateTime? startDate, DateTime? endDate)
        {
            Players = players.ToList().AsReadOnly();
            AbsenceId = absenceId;
            _defaultStartDate = startDate;
            _defaultEndDate = endDate;
            _type = type;
            Mode = AbsenceId is null ? ScreenMode.Creation : ScreenMode.Edition;
        }

        protected override void SaveCore()
        {
            if (Players.Count == 0) return;

            var result = Players.Select(x => _playerAbsenceService.Save(new AbsenceDto
            {
                PlayerId = x.PlayerId,
                Type = _type,
                Label = Label,
                EndDate = EndDate?.EndOfDay() ?? throw new NullOrEmptyException(nameof(EndDate)),
                StartDate = StartDate?.BeginningOfDay() ?? throw new NullOrEmptyException(nameof(StartDate)),
                Id = AbsenceId,
            })).First();
            Load(Players, result.Id, result.Type, result.Period.Start, result.Period.End);
        }

        protected override void RefreshCore()
        {
            if (AbsenceId is not null && _playerAbsenceService.GetById(AbsenceId.Value) is Absence item)
            {
                CreatedAt = item.CreatedAt?.ToLocalTime();
                CreatedBy = item.CreatedBy;
                ModifiedAt = item.ModifiedAt?.ToLocalTime();
                ModifiedBy = item.ModifiedBy;

                _type = item.Type;
                Label = item.Label;
                EndDate = item.Period.End.Date;
                StartDate = item.Period.Start.Date;
            }
            else
            {
                Reset();
                ValidateProperties();
            }
        }

        protected override void ResetCore()
        {
            using (ValidatePropertySuspender.Suspend())
            {
                var defaultValues = AbsenceService.New(_type);
                base.ResetCore();
                Label = defaultValues.Label.OrEmpty();
                StartDate = _defaultStartDate?.Date ?? defaultValues.StartDate.Date;
                EndDate = _defaultEndDate?.Date ?? defaultValues.EndDate.Date;
                _type = defaultValues.Type;

                CreatedAt = null;
                CreatedBy = null;
                ModifiedAt = null;
                ModifiedBy = null;
            }
        }
    }
}
