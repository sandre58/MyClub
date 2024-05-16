// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyNet.Utilities;
using MyClub.Domain.Enums;
using MyClub.Domain;


namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public interface ICompetition : IAuditableEntity, ISimilar
    {
        string Name { get; set; }

        string ShortName { get; set; }

        Category Category { get; }

        byte[]? Logo { get; set; }

        CompetitionRules Rules { get; set; }

        IEnumerable<ICompetitionSeason> Seasons { get; }

        bool RemoveSeason(ICompetitionSeason season);

        ICompetitionSeason AddSeason(ICompetitionSeason season);
    }
}

