// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class TwoLegsFormat : IRoundFormat
    {
        public static readonly TwoLegsFormat Default = new(HalfFormat.Default);
        private readonly MatchFormat[] _matchFormats = new MatchFormat[2];

        public TwoLegsFormat(HalfFormat regulationTime, HalfFormat? extraTime = null, bool useAwayGoals = false, int numberOfPenaltyShootouts = 5)
        {
            RegulationTime = regulationTime;
            ExtraTime = extraTime;
            NumberOfPenaltyShootouts = numberOfPenaltyShootouts;
            UseAwayGoals = useAwayGoals;

            _matchFormats[0] = new MatchFormat(regulationTime);
            _matchFormats[1] = new MatchFormat(regulationTime, extraTime, numberOfPenaltyShootouts);
        }

        public HalfFormat RegulationTime { get; }

        public HalfFormat? ExtraTime { get; }

        public int NumberOfPenaltyShootouts { get; }

        public bool UseAwayGoals { get; }

        public bool CanAddStage(Round round) => round.Stages.Count < 2;

        public bool CanRemoveStage(RoundStage roundStage) => false;

        public bool AllowDraw() => true;

        public MatchFormat GetFormat(RoundStage stage)
        {
            var stageIndex = stage.Stage.Stages.IndexOf(stage);

            return _matchFormats[stageIndex >= 0 && stageIndex <= 1 ? stageIndex : 0];
        }

        public bool InvertTeams(RoundStage stage)
        {
            var stageIndex = stage.Stage.Stages.IndexOf(stage);

            return stageIndex == 1;
        }

        public ExtendedResult GetExtendedResultOf(Round round, IVirtualTeam team)
        {
            var availableMatches = round.Fixtures.Find(team)?.GetAllMatches() ?? [];
            if (availableMatches.Any(x => !x.HasResult())) return ExtendedResult.None;

            var goalsFor = availableMatches.Sum(x => x.GoalsFor(team));
            var goalsAgainst = availableMatches.Sum(x => x.GoalsAgainst(team));
            var awayGoalsFor = availableMatches.Sum(x => x.Away?.Team.Id == team.Id ? x.GoalsFor(team) : 0);
            var awayGoalsAgainst = availableMatches.Sum(x => x.Home?.Team.Id == team.Id ? x.GoalsAgainst(team) : 0);
            var shootoutFor = availableMatches.Sum(x => x.ShootoutFor(team));
            var shootoutAgainst = availableMatches.Sum(x => x.ShootoutAgainst(team));

            return goalsFor > goalsAgainst ? ExtendedResult.Won
                 : goalsFor < goalsAgainst ? ExtendedResult.Lost
                 : UseAwayGoals && awayGoalsFor > awayGoalsAgainst ? ExtendedResult.Won
                 : UseAwayGoals && awayGoalsFor < awayGoalsAgainst ? ExtendedResult.Lost
                 : shootoutFor > shootoutAgainst ? ExtendedResult.WonAfterShootouts
                 : shootoutFor < shootoutAgainst ? ExtendedResult.LostAfterShootouts
                 : ExtendedResult.Drawn;

        }

        public void InitializeRound(Round round, DateTime[] dates)
        {
            round.AddStage(dates.GetByIndex(0, DateTime.Today));
            round.AddStage(dates.GetByIndex(1, DateTime.Today));
        }

        public bool CanAddFixtureInStage(RoundStage stage, Fixture fixture) => !stage.Matches.Select(x => x.Fixture).Contains(fixture);

        public bool MustUseExtraTime(MatchOfFixture match) => match.Format.ExtraTimeIsEnabled && match.Fixture.IsDraw();

        public bool MustUseShootout(MatchOfFixture match) => match.Format.ShootoutIsEnabled && match.Fixture.IsDraw();
    }
}
