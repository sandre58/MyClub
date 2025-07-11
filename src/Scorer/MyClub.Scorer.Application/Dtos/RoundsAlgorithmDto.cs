// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Scorer.Application.Dtos
{
    public abstract class RoundsAlgorithmDto
    {
        public int NumberOfTeams { get; set; }
    }

    public class ByeTeamDto : RoundsAlgorithmDto
    {
    }

    public class PreliminaryRoundDto : RoundsAlgorithmDto
    {
    }
}
