// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable.Attributes;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class RoundEditionViewModel : EntityEditionViewModel<IRound, RoundDto, RoundService>
    {
        public RoundEditionViewModel(RoundService roundService) : base(roundService)
        {
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public CupViewModel? Stage { get; private set; }

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public virtual string Name { get; set; } = string.Empty;

        [IsRequired]
        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]

        public virtual string ShortName { get; set; } = string.Empty;

        public void New(CupViewModel stage)
        {
            Stage = stage;
            New();
        }

        public void Load(IRoundViewModel round)
        {
            Stage = round.Stage;
            Load(round.Id);
        }

        protected override void ResetItem()
        {
            var defaultValues = CrudService.New(Stage?.Id);
            ShortName = defaultValues.ShortName.OrEmpty();
            Name = defaultValues.Name.OrEmpty();
        }

        protected override RoundDto ToDto()
            => new()
            {
                Id = ItemId,
                StageId = Stage?.Id,
                Name = Name,
                ShortName = ShortName,
            };

        protected override void RefreshFrom(IRound item)
        {
            Name = item.Name;
            ShortName = item.ShortName;
        }
    }
}
