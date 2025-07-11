// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class CupRepository(IProjectRepository projectRepository, IAuditService auditService) : ICupRepository
    {
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly IAuditService _auditService = auditService;

        public bool HasCurrent() => _projectRepository.HasCurrent() && _projectRepository.GetCompetition() is Cup;

        public Cup GetCurrentOrThrow() => _projectRepository.GetCompetition().CastIn<Cup>() ?? throw new InvalidOperationException($"Current competition is not cup");

        //public Round InsertRound(IRoundFormat format, DateTime[] dates, string name, string? shortName = null)
        //{
        //    var cup = GetCurrentOrThrow();
        //    var added = cup.AddRound(new Round(cup, format, dates, name, shortName));

        //    _auditService.New(added);

        //    return added;
        //}

        public void Fill(IEnumerable<Round> rounds)
        {
            var cup = GetCurrentOrThrow();
            cup.Clear();

            foreach (var item in rounds)
            {
                var round = cup.AddRound(item);
                _auditService.New(round);
            }

            _auditService.Update(cup);
        }
    }
}
