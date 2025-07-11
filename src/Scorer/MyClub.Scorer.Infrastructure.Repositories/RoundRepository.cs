// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class RoundRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Round>(projectRepository, auditService), IRoundRepository
    {
        public override IEnumerable<Round> GetAll() => CurrentProject.Competition.GetStages<Round>();

        protected override Round AddCore(Round item) => item;

        protected override IEnumerable<Round> AddRangeCore(IEnumerable<Round> items) => items.Select(AddCore);

        protected override bool RemoveCore(Round item) => item.Stage.RemoveRound(item);

        protected override int RemoveRangeCore(IEnumerable<Round> items) => items.Count(RemoveCore);
    }
}
