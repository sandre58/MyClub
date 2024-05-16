// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Domain.Enums;

namespace MyClub.DatabaseContext.Application.Dtos
{
    public class LeagueDto : CompetitionDto
    {
        public int PointsByGamesWon { get; set; }

        public int PointsByGamesDrawn { get; set; }

        public int PointsByGamesLost { get; set; }

        public List<RankingSortingColumn>? SortingColumns { get; set; }
    }
}
