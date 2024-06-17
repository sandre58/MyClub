// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.SendedMailAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using System.Linq;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class SendedMailRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<SendedMail>(projectRepository, auditService), ISendedMailRepository
    {
        public override IEnumerable<SendedMail> GetAll() => CurrentProject.SendedMails;

        protected override SendedMail AddCore(SendedMail item) => CurrentProject.AddSendedMail(item);

        protected override IEnumerable<SendedMail> AddRangeCore(IEnumerable<SendedMail> items) => items.Select(AddCore);

        protected override bool RemoveCore(SendedMail item) => CurrentProject.RemoveSendedMail(item);

        protected override int RemoveRangeCore(IEnumerable<SendedMail> items) => items.Count(RemoveCore);
    }
}
