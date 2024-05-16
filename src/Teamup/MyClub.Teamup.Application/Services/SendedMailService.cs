// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;
using MyClub.Application.Services;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.SendedMailAggregate;

namespace MyClub.Teamup.Application.Services
{
    public class SendedMailService(ISendedMailRepository repository) : CrudService<SendedMail, SendedMailDto, ISendedMailRepository>(repository)
    {
        protected override SendedMail CreateEntity(SendedMailDto dto)
        {
            var newEntity = new SendedMail(dto.Date, dto.Subject.OrEmpty(), dto.Body.OrEmpty());
            UpdateEntity(newEntity, dto);

            return newEntity;
        }

        protected override void UpdateEntity(SendedMail entity, SendedMailDto dto)
        {
            entity.Date = dto.Date;
            entity.Subject = dto.Subject.OrEmpty();
            entity.Body = dto.Body;
            entity.SendACopy = dto.SendACopy;
            entity.State = dto.State;

            if (dto.ToAddresses is not null) entity.ToAddresses.Set(dto.ToAddresses);
        }

        public SendedMail UpdateState(Guid id, SendingState state)
                => Update(id, x => x.State = state);
    }
}
