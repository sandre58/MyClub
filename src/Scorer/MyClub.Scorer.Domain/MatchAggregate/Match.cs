// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Sequences;
using MyNet.Utilities.Units;
using PropertyChanged;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class Match : AuditableEntity, IFixture, ISchedulable
    {
        public static readonly AcceptableValueRange<int> AcceptableRangeScore = new(0, int.MaxValue);
        private readonly WinnerOfMatchTeam _winnerTeam;
        private readonly LooserOfMatchTeam _looserTeam;
        private MatchOpponent? _home;
        private MatchOpponent? _away;

        public Match(DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam, MatchFormat? matchFormat = null, MatchRules? matchRules = null, Guid? id = null) : base(id)
        {
            HomeTeam = homeTeam;
            AwayTeam = awayTeam;
            Format = matchFormat ?? MatchFormat.Default;
            Rules = matchRules ?? MatchRules.Default;
            OriginDate = date;
            _winnerTeam = new(this);
            _looserTeam = new(this);

            ComputeOpponents();
        }

        public MatchFormat Format { get; set; }

        public MatchRules Rules { get; set; }

        [AlsoNotifyFor(nameof(Date))]
        public DateTime OriginDate { get; set; }

        [AlsoNotifyFor(nameof(Date))]
        public DateTime? PostponedDate { get; set; }

        public DateTime Date => PostponedDate ?? OriginDate;

        public MatchState State { get; private set; }

        public IVirtualTeam HomeTeam { get; private set; }

        public IVirtualTeam AwayTeam { get; private set; }

        public MatchOpponent? Home => _home;

        public MatchOpponent? Away => _away;

        public bool IsNeutralStadium { get; set; }

        public Stadium? Stadium { get; set; }

        public bool AfterExtraTime { get; set; }

        public IVirtualTeam GetWinnerTeam() => _winnerTeam;

        public IVirtualTeam GetLooserTeam() => _looserTeam;

        public bool ComputeOpponents() => ComputeOpponent(HomeTeam, ref _home, nameof(Home)) | ComputeOpponent(AwayTeam, ref _away, nameof(Away));

        private bool ComputeOpponent(IVirtualTeam virtualTeam, ref MatchOpponent? currentOpponent, string propertyName)
        {
            var team = virtualTeam.GetTeam();

            if (!Equals(currentOpponent?.Team, team))
            {
                currentOpponent = team is not null ? new MatchOpponent(team) : null;

                RaisePropertyChanged(propertyName);
                return true;
            }

            return false;
        }

        public void Schedule(DateTime date)
        {
            if (State == MatchState.Postponed && PostponedDate.HasValue)
                PostponedDate = date;
            else
                OriginDate = date;
        }

        public void Schedule(int offset, TimeUnit timeUnit)
        {
            if (offset == 0) return;

            if (State == MatchState.Postponed && PostponedDate.HasValue)
                PostponedDate = PostponedDate.Value.AddFluentTimeSpan(offset.Unit(timeUnit));
            else
                OriginDate = OriginDate.AddFluentTimeSpan(offset.Unit(timeUnit));
        }

        public void Reset() => Reset(MatchState.None);

        public void Cancel() => Reset(MatchState.Cancelled);

        public void Postpone(DateTime? date = null)
        {
            Reset(MatchState.Postponed);
            PostponedDate = date;
            RaisePropertyChanged(nameof(Date));
        }

        public void Start() => State = MatchState.InProgress;

        public void Suspend() => State = MatchState.Suspended;

        public void Played() => State = MatchState.Played;

        private void Reset(MatchState state)
        {
            Home?.Reset();
            Away?.Reset();
            AfterExtraTime = false;
            State = state;
        }

        public void Invert()
        {
            (HomeTeam, AwayTeam) = (AwayTeam, HomeTeam);
            (_home, _away) = (_away, _home);

            RaisePropertyChanged(nameof(Away));
            RaisePropertyChanged(nameof(Home));
        }

        public bool HasResult() => Home is not null && Away is not null && State is MatchState.Played or MatchState.InProgress or MatchState.Suspended;

        public bool HasResult(Guid teamId) => HasResult() && Participate(teamId);

        public bool IsPlayed() => State == MatchState.Played;

        public bool IsDraw() => Home is not null && Away is not null && Home.GetScore() == Away.GetScore();

        public ExtendedResult GetExtendedResultOf(Guid teamId)
            => !HasResult(teamId)
                ? ExtendedResult.None
                : GetOpponent(teamId)!.IsWithdrawn
                ? ExtendedResult.Withdrawn
                : GetOpponentAgainst(teamId)!.IsWithdrawn
                ? ExtendedResult.Won
                : GetScoreResultOf(teamId, true);

        public Result GetResultOf(Guid teamId)
            => GetExtendedResultOf(teamId) switch
            {
                ExtendedResult.Won or ExtendedResult.WonAfterShootouts => Result.Won,
                ExtendedResult.Drawn => Result.Drawn,
                ExtendedResult.Lost or ExtendedResult.Withdrawn or ExtendedResult.LostAfterShootouts => Result.Lost,
                _ => Result.None,
            };

        private ExtendedResult GetScoreResultOf(Guid teamId, bool withShootout = true)
            => !HasResult(teamId)
                ? ExtendedResult.None
                : GoalsFor(teamId) > GoalsAgainst(teamId) ? ExtendedResult.Won
                : GoalsAgainst(teamId) > GoalsFor(teamId) ? ExtendedResult.Lost
                : Format.ShootoutIsEnabled && withShootout ? GetShootoutResultOf(teamId) : ExtendedResult.Drawn;

        private ExtendedResult GetShootoutResultOf(Guid teamId)
            => !HasResult(teamId)
                ? ExtendedResult.None
                : ShootoutFor(teamId) > ShootoutAgainst(teamId) ? ExtendedResult.WonAfterShootouts
                : ShootoutAgainst(teamId) > ShootoutFor(teamId) ? ExtendedResult.LostAfterShootouts
                : ExtendedResult.Drawn;

        public Team? GetWinner()
            => Home is null || Away is null
                ? null
                : GetResultOf(Home.Team.Id) switch
                {
                    Result.Won => Home.Team,
                    Result.Lost => Away.Team,
                    _ => null,
                };

        public Team? GetLooser()
            => Home is null || Away is null
                ? null
                : GetResultOf(Home.Team.Id) switch
                {
                    Result.Won => Away.Team,
                    Result.Lost => Home.Team,
                    _ => null,
                };

        public bool IsWithdrawn(Guid teamId) => GetOpponent(teamId)?.IsWithdrawn ?? false;

        public int GoalsFor(Guid teamId) => GetOpponent(teamId)?.GetScore() ?? 0;

        public int GoalsAgainst(Guid teamId) => GetOpponentAgainst(teamId)?.GetScore() ?? 0;

        public int ShootoutFor(Guid teamId) => GetOpponent(teamId)?.GetShootoutScore() ?? 0;

        public int ShootoutAgainst(Guid teamId) => GetOpponentAgainst(teamId)?.GetShootoutScore() ?? 0;

        public bool Participate(Guid teamId) => GetTeams().Select(x => x.Id).Contains(teamId);

        public bool Participate(IVirtualTeam team) => Participate(team.Id);

        public Period GetPeriod() => new(Date, Date.AddFluentTimeSpan(Format.GetFullTime()));

        public MatchOpponent? GetOpponent(Guid teamId) => GetOpponents().GetOrDefault(teamId);

        public MatchOpponent? GetOpponentAgainst(Guid teamId) => GetOpponent(teamId) is MatchOpponent opponent ? GetOpponents().Values.FirstOrDefault(x => x.Team.Id != opponent.Team.Id) : null;

        private IEnumerable<IVirtualTeam> GetTeams() => new List<IVirtualTeam?>() { HomeTeam, AwayTeam, Home?.Team, Away?.Team, _winnerTeam, _looserTeam }.NotNull().Distinct();

        private Dictionary<Guid, MatchOpponent> GetOpponents() => new List<MatchOpponent?>() { Home, Away }.NotNull().ToDictionary(x => x.Team.Id, x => x);

        public void SetScore(int homeScore, int awayScore, bool afterExtraTime = false, int? homeShootoutScore = null, int? awayShootoutScore = null)
        {
            if (Home is null || Away is null) return;

            var hasWithdraw = Home.IsWithdrawn || Away.IsWithdrawn;

            AfterExtraTime = Format.ExtraTimeIsEnabled && !hasWithdraw && afterExtraTime;

            Home.SetScore(homeScore, Format.ShootoutIsEnabled && !hasWithdraw ? homeShootoutScore : null);
            Away.SetScore(awayScore, Format.ShootoutIsEnabled && !hasWithdraw ? awayShootoutScore : null);
        }

        public void SetScore(IEnumerable<Goal> homeGoals, IEnumerable<Goal> awayGoals, bool afterExtraTime = false, IEnumerable<PenaltyShootout>? homeShootouts = null, IEnumerable<PenaltyShootout>? awayShootouts = null)
        {
            if (Home is null || Away is null) return;
            var hasWithdraw = Home.IsWithdrawn || Away.IsWithdrawn;

            AfterExtraTime = Format.ExtraTimeIsEnabled && !hasWithdraw && afterExtraTime;

            Home.SetScore(homeGoals, Format.ShootoutIsEnabled && !hasWithdraw ? homeShootouts : null);
            Away.SetScore(awayGoals, Format.ShootoutIsEnabled && !hasWithdraw ? awayShootouts : null);
        }

        public override string ToString()
        {
            var str = new StringBuilder($"{Date:G} | {HomeTeam} vs {AwayTeam}");

            if (Home is not null && Away is not null && (State == MatchState.Played || State == MatchState.InProgress || State == MatchState.Suspended))
            {
                if (Home.IsWithdrawn)
                    str.Append($"{Home.Team} is withdrawn");
                else if (Away.IsWithdrawn)
                    str.Append($"{Away.Team} is withdrawn");
                else
                {
                    str.Append($" : {Home.GetScore()}-{Away.GetScore()}");

                    if (Home.GetScore() == Away.GetScore())
                    {
                        if (Format.ShootoutIsEnabled)
                        {
                            str.Append($" ({Home.GetShootoutScore()}-{Away.GetShootoutScore()})");
                        }
                    }
                    else if (AfterExtraTime && Format.ExtraTimeIsEnabled)
                        str.Append(" e");
                }
            }

            return str.ToString();
        }
    }
}
