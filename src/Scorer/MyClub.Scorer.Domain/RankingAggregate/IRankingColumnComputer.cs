// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Scorer.Domain.RankingAggregate
{
    public interface IRankingColumnComputer
    {
        object Compute(RankingRow row);
    }

    public interface IRankingColumnComputer<out T>
    {
        T Compute(RankingRow row);
    }
}
