// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyNet.Utilities;
using MyClub.Application.Services;
using MyClub.Teamup.Application.Dtos;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.TacticAggregate;

namespace MyClub.Teamup.Application.Services
{
    public class TacticService(ITacticRepository repository) : CrudService<Tactic, TacticDto, ITacticRepository>(repository)
    {
        protected override Tactic CreateEntity(TacticDto dto)
        {
            var newEntity = new Tactic(dto.Label.OrEmpty());
            UpdateEntity(newEntity, dto);

            return newEntity;
        }

        protected override void UpdateEntity(Tactic entity, TacticDto dto)
        {
            entity.Code = dto.Code.OrEmpty();
            entity.Label = dto.Label.OrEmpty();
            entity.Order = dto.Order;
            entity.Description = dto.Description;
            if (dto.Positions is not null)
            {
                entity.Positions.ToList().ForEach(x => entity.RemovePosition(x.Position));
                dto.Positions.ForEach(x =>
                {
                    var position = new TacticPosition(x.Position!)
                    {
                        Number = x.Number,
                        OffsetX = x.OffsetX,
                        OffsetY = x.OffsetY,
                    };
                    position.Instructions.AddRange(x.Instructions);

                    entity.AddPosition(position);
                });
            }
            if (dto.Instructions is not null) entity.Instructions.Set(dto.Instructions);
        }

        public void SetOrder(Guid id, int value) => Update(id, x => x.Order = value);

        public TacticDto NewTactic()
        {
            var label = MyClubResources.NewTactic.Increment(GetAll().Select(x => x.Label), format: " (#)");
            return new()
            {
                Label = label
            };
        }
    }
}
