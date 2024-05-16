// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TrainingAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class TrainingSessionRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<TrainingSession>(projectRepository, auditService), ITrainingSessionRepository
    {
        public override IEnumerable<TrainingSession> GetAll() => CurrentProject.TrainingSessions;

        protected override TrainingSession AddCore(TrainingSession item) => CurrentProject.AddTrainingSession(item);

        protected override bool DeleteCore(TrainingSession item) => CurrentProject.RemoveTrainingSession(item);
    }
}
