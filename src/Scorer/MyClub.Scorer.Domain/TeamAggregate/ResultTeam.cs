// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.TeamAggregate
{
    public class ResultTeam<T> : Entity, IVirtualTeam
        where T : IFixture
    {
        private readonly Result _matchResult;

        public ResultTeam(T fixture, Result matchResult)
        {
            _matchResult = matchResult;
            Fixture = fixture;
        }

        public T Fixture { get; }

        public Team? GetTeam()
            => !Fixture.IsPlayed()
                ? null
                : _matchResult switch
                {
                    Result.Won => Fixture.GetWinner(),
                    Result.Lost => Fixture.GetLooser(),
                    _ => null,
                };

        public bool IsSimilar(object? obj) => obj is ResultTeam<T> winnerOfMatchTeam && Equals(winnerOfMatchTeam.Fixture, Fixture) && winnerOfMatchTeam._matchResult == _matchResult;

        public override string ToString() => $"{_matchResult} | {Fixture}";
    }
}
