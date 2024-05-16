// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
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
using MyClub.Teamup.Domain.Extensions;
using MyClub.Teamup.Domain.Factories.Extensions;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class InjuryEditionViewModel : EditionViewModel
    {
        private readonly InjuryService _injuryService;

        public PlayerViewModel? Player { get; private set; }

        public Guid? ItemId { get; private set; }

        [IsRequired]
        [Display(Name = nameof(Condition), ResourceType = typeof(MyClubResources))]
        public virtual string Condition { get; set; } = string.Empty;

        [IsRequired]
        [Display(Name = nameof(Severity), ResourceType = typeof(MyClubResources))]
        public virtual InjurySeverity Severity { get; set; }

        [IsRequired]
        [Display(Name = nameof(Type), ResourceType = typeof(MyClubResources))]
        public virtual InjuryType Type { get; set; }

        [IsRequired]
        [Display(Name = nameof(Category), ResourceType = typeof(MyClubResources))]
        public virtual InjuryCategory Category { get; set; }

        [Display(Name = nameof(Description), ResourceType = typeof(MyClubResources))]
        public virtual string? Description { get; set; }

        [IsRequired]
        [ValidateProperty(nameof(EndDate))]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public virtual DateTime? Date { get; set; }

        [ValidateProperty(nameof(Date))]
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

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public DateTime DisplayDate { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public DateTime DisplayReturnDate { get; set; }

        public InjuryEditionViewModel(InjuryService injuryService)
        {
            _injuryService = injuryService;

            ValidationRules.AddNotNull<InjuryEditionViewModel, DateTime?>(x => x.Date, MessageResources.FieldStartDateMustBeLowerOrEqualsThanEndDateError, x => !x.HasValue || !EndDate.HasValue || x.Value.Date <= EndDate.Value.Date);
            ValidationRules.Add<InjuryEditionViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !Date.HasValue || !x.HasValue || Date.Value.Date <= x.Value.Date);

            Disposables.AddRange(
            [
                this.WhenPropertyChanged(x => x.Type, false).Subscribe(_ =>
                {
                    Condition = Type.GetDefaultCondition();
                    Category = Type.ToDefaultCategory();
                }),

                this.WhenPropertyChanged(x => x.Date).Subscribe(_ =>
                {
                    if (Date.HasValue && EndDate.HasValue && Date >= EndDate.Value)
                        EndDate = Date.Value.AddDays(1);
                })
            ]);
        }

        public void Load(Guid? id) => throw new NotImplementedException();

        public void Load(PlayerViewModel player, Guid? injuryId)
        {
            Player = player;
            ItemId = injuryId;
            Mode = ItemId is null ? ScreenMode.Creation : ScreenMode.Edition;
        }

        protected override void SaveCore()
        {
            if (Player is null) return;

            var result = _injuryService.Save(new InjuryDto
            {
                PlayerId = Player.Id,
                Category = Category,
                Condition = Condition,
                Date = Date?.ToUniversalTime() ?? throw new NullOrEmptyException(nameof(Date)),
                Description = Description,
                EndDate = EndDate?.Date,
                Id = ItemId,
                Severity = Severity,
                Type = Type
            });
            Load(Player, result.Id);
        }

        protected override void RefreshCore()
        {
            if (ItemId is not null && _injuryService.GetById(ItemId.Value) is Injury item)
            {
                CreatedAt = item.CreatedAt?.ToLocalTime();
                CreatedBy = item.CreatedBy;
                ModifiedAt = item.ModifiedAt?.ToLocalTime();
                ModifiedBy = item.ModifiedBy;
                Type = item.Type;
                Condition = item.Condition;
                Severity = item.Severity;
                Date = item.Period.Start.ToLocalTime();
                EndDate = item.Period.End?.Date;
                Category = item.Category;
                Description = item.Description;

                DisplayDate = item.Period.Start.Date;
                DisplayReturnDate = item.Period.End?.Date ?? DateTime.Today;
            }
            else
            {
                Reset();
            }
        }

        protected override void ResetCore()
        {
            using (ValidatePropertySuspender.Suspend())
            {
                var defaultValues = InjuryService.New();
                base.ResetCore();
                Type = defaultValues.Type;
                Condition = defaultValues.Condition.OrEmpty();
                Severity = defaultValues.Severity;
                Date = defaultValues.Date.ToLocalTime();
                EndDate = defaultValues.EndDate?.Date;
                Category = defaultValues.Category;
                Description = defaultValues.Description;

                CreatedAt = null;
                CreatedBy = null;
                ModifiedAt = null;
                ModifiedBy = null;

                DisplayDate = DateTime.Today;
                DisplayReturnDate = DateTime.Today;
            }
        }
    }
}
