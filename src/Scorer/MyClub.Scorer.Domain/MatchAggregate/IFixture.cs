// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public interface IFixture
    {
        bool IsPlayed();

        Result GetResultOf(Guid teamId);

        Team? GetWinner();

        Team? GetLooser();

        IVirtualTeam GetWinnerTeam();

        IVirtualTeam GetLooserTeam();

        bool Participate(Guid teamId);
    }
}
