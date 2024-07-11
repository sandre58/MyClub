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
    public class Match : AuditableEntity, ISchedulable
    {
        public static readonly AcceptableValueRange<int> AcceptableRangeScore = new(0, int.MaxValue);

        private readonly Dictionary<ITeam, MatchOpponent> _opponents;

        public Match(DateTime date, ITeam homeTeam, ITeam awayTeam, MatchFormat matchFormat, Guid? id = null) : base(id)
        {
            _opponents = new Dictionary<ITeam, MatchOpponent>()
            {
                { homeTeam, new(homeTeam) },
                { awayTeam, new(awayTeam) }
            };
            Format = matchFormat;
            OriginDate = date;
        }

        public MatchFormat Format { get; set; }

        [AlsoNotifyFor(nameof(Date))]
        public DateTime OriginDate { get; set; }

        [AlsoNotifyFor(nameof(Date))]
        public DateTime? PostponedDate { get; set; }

        public DateTime Date => PostponedDate ?? OriginDate;

        public MatchState State { get; private set; }

        public ITeam HomeTeam => _opponents.First().Key;

        public ITeam AwayTeam => _opponents.Last().Key;

        public MatchOpponent Home => _opponents.First().Value;

        public MatchOpponent Away => _opponents.Last().Value;

        public bool IsNeutralStadium { get; set; }

        public Stadium? Stadium { get; set; }

        public bool AfterExtraTime { get; set; }

        public void Cancel() => Reset(MatchState.Cancelled);

        public void Postpone(DateTime? date = null)
        {
            Reset(MatchState.Postponed);
            PostponedDate = date;
            RaisePropertyChanged(nameof(Date));
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

        public void Start() => Reset(MatchState.InProgress);

        public void Suspend() => Reset(MatchState.Suspended);

        public void Played() => State = MatchState.Played;

        private void Reset(MatchState state)
        {
            Home.Reset();
            Away.Reset();
            AfterExtraTime = false;
            State = state;
        }

        public void Invert()
        {
            var home = Home;
            var away = Away;

            _opponents.Clear();
            _opponents.Add(away.Team, away);
            _opponents.Add(home.Team, home);

            RaisePropertyChanged(nameof(HomeTeam));
            RaisePropertyChanged(nameof(AwayTeam));
        }

        public void SetScore(int homeScore, int awayScore, bool afterExtraTime = false, int? homeShootoutScore = null, int? awayShootoutScore = null)
        {
            Home.SetScore(homeScore);
            Away.SetScore(awayScore);

            if (Home.IsWithdrawn || Away.IsWithdrawn) return;

            AfterExtraTime = Format.ExtraTimeIsEnabled && afterExtraTime;

            Home.SetScore(homeScore, Format.ShootoutIsEnabled ? homeShootoutScore : null);
            Away.SetScore(awayScore, Format.ShootoutIsEnabled ? awayShootoutScore : null);
        }

        public bool IsDraw() => Home.GetScore() == Away.GetScore();

        public MatchResultDetailled GetDetailledResultOf(ITeam team)
        {
            if (State != MatchState.Played && State != MatchState.InProgress || !Participate(team)) return MatchResultDetailled.None;
            if (Home.IsWithdrawn && HomeTeam == team) return MatchResultDetailled.Withdrawn;
            if (Home.IsWithdrawn && AwayTeam == team) return MatchResultDetailled.Won;
            if (Away.IsWithdrawn && HomeTeam == team) return MatchResultDetailled.Won;
            if (Away.IsWithdrawn && AwayTeam == team) return MatchResultDetailled.Withdrawn;
            if (Home.GetScore() > Away.GetScore() && HomeTeam == team) return MatchResultDetailled.Won;
            if (Home.GetScore() < Away.GetScore() && HomeTeam == team) return MatchResultDetailled.Lost;
            if (Home.GetScore() < Away.GetScore() && AwayTeam == team) return MatchResultDetailled.Won;
            if (Home.GetScore() > Away.GetScore() && AwayTeam == team) return MatchResultDetailled.Lost;
            if (Format.ShootoutIsEnabled)
            {
                if (Home.GetShootoutScore() > Away.GetShootoutScore() && HomeTeam == team) return MatchResultDetailled.WonAfterShootouts;
                if (Home.GetShootoutScore() < Away.GetShootoutScore() && HomeTeam == team) return MatchResultDetailled.LostAfterShootouts;
                if (Home.GetShootoutScore() < Away.GetShootoutScore() && AwayTeam == team) return MatchResultDetailled.WonAfterShootouts;
                if (Home.GetShootoutScore() > Away.GetShootoutScore() && AwayTeam == team) return MatchResultDetailled.LostAfterShootouts;
            }

            return MatchResultDetailled.Drawn;
        }

        public MatchResultDetailled GetDetailledResultOf(Guid id) => Participate(id) ? GetDetailledResultOf(_opponents.Keys.GetById(id)) : MatchResultDetailled.None;

        public MatchResult GetResultOf(ITeam team)
            => GetDetailledResultOf(team) switch
            {
                MatchResultDetailled.Won or MatchResultDetailled.WonAfterShootouts => MatchResult.Won,
                MatchResultDetailled.Drawn => MatchResult.Drawn,
                MatchResultDetailled.Lost or MatchResultDetailled.Withdrawn or MatchResultDetailled.LostAfterShootouts => MatchResult.Lost,
                _ => MatchResult.None,
            };

        public MatchResult GetResultOf(Guid id) => Participate(id) ? GetResultOf(_opponents.Keys.GetById(id)) : MatchResult.None;

        public ITeam? GetWinner() => GetResultOf(HomeTeam) == MatchResult.Won ? HomeTeam : GetResultOf(AwayTeam) == MatchResult.Won ? AwayTeam : null;

        public ITeam? GetLooser() => GetResultOf(HomeTeam) == MatchResult.Lost ? HomeTeam : GetResultOf(AwayTeam) == MatchResult.Lost ? AwayTeam : null;

        public bool IsWonBy(ITeam team) => GetResultOf(team) == MatchResult.Won;

        public bool IsWonBy(Guid id) => Participate(id) && IsWonBy(_opponents.Keys.GetById(id));

        public bool IsLostBy(ITeam team) => GetResultOf(team) == MatchResult.Lost;

        public bool IsLostBy(Guid id) => Participate(id) && IsLostBy(_opponents.Keys.GetById(id));

        public int GoalsFor(ITeam team) => _opponents.TryGetValue(team, out var value) ? value.GetScore() : 0;

        public int GoalsFor(Guid id) => Participate(id) ? GoalsFor(_opponents.Keys.GetById(id)) : 0;

        public int GoalsAgainst(ITeam team) => HomeTeam == team ? Away.GetScore() : AwayTeam == team ? Home.GetScore() : 0;

        public int GoalsAgainst(Guid id) => Participate(id) ? GoalsAgainst(_opponents.Keys.GetById(id)) : 0;

        public bool IsWithdrawn(ITeam team) => Participate(team) && _opponents[team].IsWithdrawn;

        public bool IsWithdrawn(Guid id) => Participate(id) && IsWithdrawn(_opponents.Keys.GetById(id));

        public bool Participate(ITeam team) => _opponents.ContainsKey(team);

        public bool Participate(Guid teamId) => _opponents.Keys.Any(x => x.Id == teamId);

        public MatchOpponent? GetOpponent(Guid teamId) => _opponents.FirstOrDefault(x => x.Key.Id == teamId).Value;

        public Period GetPeriod() => new(Date, Date.AddFluentTimeSpan(Format.GetFullTime()));

        public override string ToString()
        {
            var str = new StringBuilder($"{Date:G} | {HomeTeam} vs {AwayTeam}");

            if (State == MatchState.Played || State == MatchState.InProgress || State == MatchState.Suspended)
            {
                if (Home.IsWithdrawn)
                    str.Append($"{HomeTeam} is withdrawn");
                else if (Away.IsWithdrawn)
                    str.Append($"{AwayTeam} is withdrawn");
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
