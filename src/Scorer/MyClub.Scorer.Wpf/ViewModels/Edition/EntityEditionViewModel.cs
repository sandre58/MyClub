// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Application.Dtos;
using MyClub.Application.Services;
using MyClub.Domain;
using MyNet.Observable.Attributes;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    [CanBeValidatedForDeclaredClassOnly(false)]
    [CanSetIsModifiedAttributeForDeclaredClassOnly(false)]
    internal abstract class EntityEditionViewModel<T, TDto, TCrudService> : EditionViewModel, IEntityEditionViewModel
        where TCrudService : ICrudService<T, TDto>
        where T : IAuditableEntity
        where TDto : EntityDto
    {
        private Action? _initializeOnCreation;

        protected TCrudService CrudService { get; }

        public Guid? ItemId { get; private set; }

        public DateTime? CreatedAt { get; private set; }

        public string? CreatedBy { get; private set; }

        public DateTime? ModifiedAt { get; private set; }

        public string? ModifiedBy { get; private set; }

        protected EntityEditionViewModel(TCrudService crudService) => CrudService = crudService;

        public void Load(Guid id)
        {
            _initializeOnCreation = null;
            ItemId = id;
            Mode = ScreenMode.Edition;
        }

        public void New(Action? initialize = null)
        {
            _initializeOnCreation = initialize;
            ItemId = null;
            Mode = ScreenMode.Creation;
        }

        protected override void SaveCore()
        {
            var result = CrudService.Save(ToDto());
            Load(result.Id);
        }

        protected override void RefreshCore()
        {
            if (ItemId is not null && CrudService.GetById(ItemId.Value) is T item)
            {
                CreatedAt = item.CreatedAt?.ToLocalTime();
                CreatedBy = item.CreatedBy;
                ModifiedAt = item.ModifiedAt?.ToLocalTime();
                ModifiedBy = item.ModifiedBy;

                RefreshFrom(item);
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
                CreatedAt = null;
                CreatedBy = null;
                ModifiedAt = null;
                ModifiedBy = null;
                ResetItem();

                if (Mode == ScreenMode.Creation)
                    _initializeOnCreation?.Invoke();
            }
        }

        protected abstract void ResetItem();

        protected abstract TDto ToDto();

        protected abstract void RefreshFrom(T item);
    }
}
