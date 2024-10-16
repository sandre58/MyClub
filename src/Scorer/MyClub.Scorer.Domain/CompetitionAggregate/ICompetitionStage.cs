// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface ICompetitionStage : IStage
    {
        string Name { get; set; }

        string ShortName { get; set; }
    }
}
