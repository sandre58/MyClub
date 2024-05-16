// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.Locators;
using MyNet.Utilities;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.Factories.Extensions;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services
{
    internal class TacticPresentationService(TacticService service, IViewModelLocator viewModelLocator) : PresentationServiceBase<TacticViewModel, TacticEditionViewModel, TacticService>(service, viewModelLocator)
    {
        public async Task<Guid?> AddAsync(KnownTactic knownTactic)
            => await AddAsync(x =>
            {
                var tactic = knownTactic.CreateTactic();
                x.Code = tactic.Code;
                x.Description = tactic.Description;
                x.InstructionsViewModel.SetSource(tactic.Instructions);
                x.Label = tactic.Label.Increment(Service.GetAll().Select(x => x.Label), format: " (#)");
                x.UpdatePositions(tactic.Positions.Select(x =>
                    new TacticPositionDto
                    {
                        Id = x.Id,
                        Number = x.Number,
                        OffsetX = x.OffsetX,
                        OffsetY = x.OffsetY,
                        Instructions = [.. x.Instructions],
                        Position = x.Position
                    }));
            }).ConfigureAwait(false);

        public async Task<Guid?> DuplicateAsync(TacticViewModel item)
            => await AddAsync(x =>
            {
                var itemToDuplicated = Service.GetById(item.Id);

                if (itemToDuplicated is not null)
                {
                    x.Code = itemToDuplicated.Code;
                    x.Description = itemToDuplicated.Description;
                    x.InstructionsViewModel.SetSource(itemToDuplicated.Instructions);
                    x.Label = itemToDuplicated.Label.Increment(Service.GetAll().Select(x => x.Label), format: " (#)");
                    x.UpdatePositions(itemToDuplicated.Positions.Select(x =>
                    new TacticPositionDto
                    {
                        Id = x.Id,
                        Number = x.Number,
                        OffsetX = x.OffsetX,
                        OffsetY = x.OffsetY,
                        Instructions = [.. x.Instructions],
                        Position = x.Position
                    }));
                }
            }).ConfigureAwait(false);

        public void SetOrder(TacticViewModel tacticViewModel, int value) => Service.SetOrder(tacticViewModel.Id, value);
    }
}
