// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Extensions;
using System.Linq;
using System;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class NumberOfWinsFormat : IRoundFormat
    {
        public static readonly NumberOfWinsFormat Default = new(2, [false, true], HalfFormat.Default);
        private readonly MatchFormat _matchFormat = MatchFormat.NoDraw;

        public NumberOfWinsFormat(int numberOfWins, bool[] invertTeamsByStage, HalfFormat regulationTime, HalfFormat? extraTime = null, int? numberOfPenaltyShootouts = null)
        {
            NumberOfWins = numberOfWins;
            RegulationTime = regulationTime;
            ExtraTime = extraTime;
            NumberOfPenaltyShootouts = numberOfPenaltyShootouts;
            InvertTeamsByStage = invertTeamsByStage;

            _matchFormat = new MatchFormat(regulationTime);
        }

        public HalfFormat RegulationTime { get; }

        public HalfFormat? ExtraTime { get; }

        public int? NumberOfPenaltyShootouts { get; }

        public int NumberOfWins { get; }

        public bool[] InvertTeamsByStage { get; set; } = [false, true];

        public int GetMaximumOfStages() => NumberOfWins * 2 - 1;

        public bool CanAddStage(Round round) => round.Stages.Count <= GetMaximumOfStages();

        public bool CanRemoveStage(RoundStage roundStage) => roundStage.Stage.Stages.IndexOf(roundStage) >= NumberOfWins;

        public bool AllowDraw() => false;

        public MatchFormat GetFormat(RoundStage stage) => _matchFormat;

        public bool InvertTeams(RoundStage stage)
        {
            var stageIndex = stage.Stage.Stages.IndexOf(stage);

            return InvertTeamsByStage.GetByIndex(stageIndex, false);
        }

        public ExtendedResult GetExtendedResultOf(Round round, IVirtualTeam team)
        {
            var availableMatches = round.Fixtures.Find(team)?.GetAllMatches().ToList() ?? [];
            if (availableMatches.Any(x => !x.HasResult())) return ExtendedResult.None;

            var wins = availableMatches.Count(x => x.IsWonBy(team));
            var lost = availableMatches.Count(x => x.IsLostBy(team));

            return wins >= NumberOfWins ? ExtendedResult.Won
                 : lost >= NumberOfWins ? ExtendedResult.Lost
                 : ExtendedResult.None;
        }

        public void InitializeRound(Round round, DateTime[] dates)
        {
            for (var i = 0; i < NumberOfWins; i++)
                round.AddStage(dates.GetByIndex(0, DateTime.Today));
        }

        public bool CanAddFixtureInStage(RoundStage stage, Fixture fixture)
        {
            var stageIndex = stage.Stage.Stages.IndexOf(stage);

            return !stage.Matches.Select(x => x.Fixture).Contains(fixture) && (stageIndex < NumberOfWins || (fixture.IsPlayed() && fixture.GetWinner() is null));
        }

        public bool MustUseExtraTime(MatchOfFixture match) => match.Format.ExtraTimeIsEnabled && match.IsDraw();

        public bool MustUseShootout(MatchOfFixture match) => match.Format.ShootoutIsEnabled && match.IsDraw();
    }
}
