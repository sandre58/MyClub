// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public interface IFixture
    {
        bool IsPlayed();

        bool IsDraw();

        Result GetResultOf(IVirtualTeam team);

        Team? GetWinner();

        Team? GetLooser();

        IVirtualTeam GetWinnerTeam();

        IVirtualTeam GetLooserTeam();

        bool Participate(IVirtualTeam team);
    }
}
