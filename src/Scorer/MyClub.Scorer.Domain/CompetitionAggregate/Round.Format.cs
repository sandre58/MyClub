// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface IRoundFormat
    {
        bool CanAddStage(Round round);

        bool CanRemoveStage(RoundStage roundStage);

        bool CanAddFixtureInStage(RoundStage stage, Fixture fixture);

        bool AllowDraw();

        ExtendedResult GetExtendedResultOf(Round round, IVirtualTeam team);

        MatchFormat GetFormat(RoundStage stage);

        bool InvertTeams(RoundStage stage);

        void InitializeRound(Round round, DateTime[] dates);

        bool MustUseExtraTime(MatchOfFixture match);

        bool MustUseShootout(MatchOfFixture match);
    }
}
