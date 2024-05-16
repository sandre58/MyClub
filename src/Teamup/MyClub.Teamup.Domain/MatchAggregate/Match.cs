// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Sequences;
using PropertyChanged;

namespace MyClub.Teamup.Domain.MatchAggregate
{
    public class Match : AuditableEntity
    {
        public static readonly AcceptableValueRange<int> AcceptableRangeScore = new(0, int.MaxValue);

        private readonly Dictionary<Team, MatchOpponent> _opponents;

        public Match(DateTime date, Team homeTeam, Team awayTeam, MatchFormat matchFormat, Guid? id = null) : base(id)
        {
            _opponents = new Dictionary<Team, MatchOpponent>()
            {
                { homeTeam, new(homeTeam) },
                { awayTeam, new(awayTeam) }
            };
            Format = matchFormat;
            OriginDate = date;
            Stadium = HomeTeam.Stadium.Value;
        }

        public MatchFormat Format { get; set; }

        [AlsoNotifyFor(nameof(Date))]
        public DateTime OriginDate { get; set; }

        [AlsoNotifyFor(nameof(Date))]
        public DateTime? PostponedDate { get; set; }

        public DateTime Date => PostponedDate ?? OriginDate;

        public MatchState State { get; private set; }

        public Team HomeTeam => _opponents.First().Key;

        public Team AwayTeam => _opponents.Last().Key;

        public MatchOpponent Home => _opponents.First().Value;

        public MatchOpponent Away => _opponents.Last().Value;

        public bool NeutralVenue { get; set; }

        public Stadium? Stadium { get; set; }

        public bool AfterExtraTime { get; set; }

        public void Cancel() => Reset(MatchState.Cancelled);

        public void Postpone(DateTime? date = null)
        {
            Reset(MatchState.Postponed);
            PostponedDate = date;
            RaisePropertyChanged(nameof(Date));
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

            if (!NeutralVenue)
                Stadium = HomeTeam.Stadium.Value;

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

        public MatchResult GetResultOf(Team team)
        {
            if (State != MatchState.Played && State != MatchState.InProgress || !Participate(team)) return MatchResult.None;
            if (Home.IsWithdrawn && HomeTeam == team) return MatchResult.Lost;
            if (Home.IsWithdrawn && AwayTeam == team) return MatchResult.Won;
            if (Away.IsWithdrawn && HomeTeam == team) return MatchResult.Won;
            if (Away.IsWithdrawn && AwayTeam == team) return MatchResult.Lost;
            if (Home.GetScore() > Away.GetScore() && HomeTeam == team) return MatchResult.Won;
            if (Home.GetScore() < Away.GetScore() && HomeTeam == team) return MatchResult.Lost;
            if (Home.GetScore() < Away.GetScore() && AwayTeam == team) return MatchResult.Won;
            if (Home.GetScore() > Away.GetScore() && AwayTeam == team) return MatchResult.Lost;
            if (Format.ShootoutIsEnabled)
            {
                if (Home.GetShootoutScore() > Away.GetShootoutScore() && HomeTeam == team) return MatchResult.Won;
                if (Home.GetShootoutScore() < Away.GetShootoutScore() && HomeTeam == team) return MatchResult.Lost;
                if (Home.GetShootoutScore() < Away.GetShootoutScore() && AwayTeam == team) return MatchResult.Won;
                if (Home.GetShootoutScore() > Away.GetShootoutScore() && AwayTeam == team) return MatchResult.Lost;
            }

            return MatchResult.Drawn;
        }

        public MatchResult GetResultOf(Guid id) => Participate(id) ? GetResultOf(_opponents.Keys.GetById(id)) : MatchResult.None;

        public Team? GetWinner() => GetResultOf(HomeTeam) == MatchResult.Won ? HomeTeam : GetResultOf(AwayTeam) == MatchResult.Won ? AwayTeam : null;

        public Team? GetLooser() => GetResultOf(HomeTeam) == MatchResult.Lost ? HomeTeam : GetResultOf(AwayTeam) == MatchResult.Lost ? AwayTeam : null;

        public bool IsWonBy(Team team) => GetResultOf(team) == MatchResult.Won;

        public bool IsLostBy(Team team) => GetResultOf(team) == MatchResult.Lost;

        public int GoalsFor(Team team) => _opponents.TryGetValue(team, out var value) ? value.GetScore() : 0;

        public int GoalsAgainst(Team team) => HomeTeam == team ? Away.GetScore() : AwayTeam == team ? Home.GetScore() : 0;

        public bool IsWithdrawn(Team team) => Participate(team) && _opponents[team].IsWithdrawn;

        public bool Participate(Team team) => _opponents.ContainsKey(team);

        public bool Participate(Guid teamId) => _opponents.Keys.Any(x => x.Id == teamId);

        public MatchOpponent? GetOpponent(Guid teamId) => _opponents.FirstOrDefault(x => x.Key.Id == teamId).Value;

        public int GetPenaltyPoints(Team team) => Participate(team) ? _opponents[team].PenaltyPoints : 0;

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
