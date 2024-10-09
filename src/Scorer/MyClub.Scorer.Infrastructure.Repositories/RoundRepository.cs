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
    public class RoundRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<IRound>(projectRepository, auditService), IRoundRepository
    {
        public override IEnumerable<IRound> GetAll() => CurrentProject.Competition.GetStages<IRound>();

        protected override IRound AddCore(IRound item) => item;

        protected override IEnumerable<IRound> AddRangeCore(IEnumerable<IRound> items) => items.Select(AddCore);

        protected override bool RemoveCore(IRound item) => item.Stage.RemoveRound(item);

        protected override int RemoveRangeCore(IEnumerable<IRound> items) => items.Count(RemoveCore);
    }
}
