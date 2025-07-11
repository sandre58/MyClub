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
    public class OneLegFormat : IRoundFormat
    {
        public static readonly OneLegFormat Default = new(HalfFormat.Default);
        private readonly MatchFormat _matchFormat;

        public OneLegFormat(HalfFormat regulationTime, HalfFormat? extraTime = null, int numberOfPenaltyShootouts = 5)
        {
            RegulationTime = regulationTime;
            ExtraTime = extraTime;
            NumberOfPenaltyShootouts = numberOfPenaltyShootouts;

            _matchFormat = new MatchFormat(regulationTime, extraTime, numberOfPenaltyShootouts);
        }

        public HalfFormat RegulationTime { get; }

        public HalfFormat? ExtraTime { get; }

        public int NumberOfPenaltyShootouts { get; }

        public bool CanAddStage(Round round) => round.Stages.Count < 1;

        public bool CanRemoveStage(RoundStage roundStage) => false;

        public bool AllowDraw() => true;

        public MatchFormat GetFormat(RoundStage stage) => _matchFormat;

        public bool InvertTeams(RoundStage stage) => false;

        public ExtendedResult GetExtendedResultOf(Round round, IVirtualTeam team)
        {
            var match = round.Fixtures.Find(team)?.GetAllMatches().FirstOrDefault();
            return match?.GetExtendedResultOf(team) ?? ExtendedResult.None;
        }

        public void InitializeRound(Round round, DateTime[] dates) => round.AddStage(dates.GetByIndex(0, DateTime.Today));

        public bool CanAddFixtureInStage(RoundStage stage, Fixture fixture) => !stage.Matches.Select(x => x.Fixture).Contains(fixture);

        public bool MustUseExtraTime(MatchOfFixture match) => match.Format.ExtraTimeIsEnabled && match.IsDraw();

        public bool MustUseShootout(MatchOfFixture match) => match.Format.ShootoutIsEnabled && match.IsDraw();
    }
}
