// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MyClub.Teamup.Plugins.Contracts.Dtos
{
    public class LeagueDto : CompetitionDto
    {
        public int PointsByGamesWon { get; set; }

        public int PointsByGamesDrawn { get; set; }

        public int PointsByGamesLost { get; set; }

        public List<string>? SortingColumns { get; set; }
    }
}
