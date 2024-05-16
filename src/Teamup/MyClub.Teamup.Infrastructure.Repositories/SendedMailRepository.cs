// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.SendedMailAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class SendedMailRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<SendedMail>(projectRepository, auditService), ISendedMailRepository
    {
        public override IEnumerable<SendedMail> GetAll() => CurrentProject.SendedMails;

        protected override SendedMail AddCore(SendedMail item) => CurrentProject.AddSendedMail(item);

        protected override bool DeleteCore(SendedMail item) => CurrentProject.RemoveSendedMail(item);
    }
}
