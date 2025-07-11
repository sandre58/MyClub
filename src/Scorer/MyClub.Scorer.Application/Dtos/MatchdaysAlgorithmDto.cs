// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Scorer.Application.Dtos
{
    public abstract class MatchdaysAlgorithmDto
    {
        public int NumberOfTeams { get; set; }
    }

    public class RoundRobinDto : MatchdaysAlgorithmDto
    {
        public bool[]? MatchesBetweenTeams { get; set; }
    }

    public class SwissSystemDto : MatchdaysAlgorithmDto
    {
        public int NumberOfMatchesByTeam { get; set; }
    }
}
