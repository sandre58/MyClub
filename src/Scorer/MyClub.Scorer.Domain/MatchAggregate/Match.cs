// -----------------------------------------------------------------------
// <copyright file="Match.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchAggregate.MatchEvents;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Sequences;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Domain.MatchAggregate;

public class Match : AuditableEntity<MatchId>, IMatch, IAggregateRoot
{
    public static readonly AcceptableValueRange<int> AcceptableRangeScore = new(0, int.MaxValue);

    // <remarks>Used by EF Core</remarks>
    private Match()
        : base()
    {
        Home = null!;
        Away = null!;
        Format = null!;
        Rules = null!;
    }

    private Match(MatchId id, DateTime date, TeamReference homeTeam, TeamReference awayTeam, MatchFormat matchFormat, MatchRules matchRules)
        : base(id)
    {
        Format = matchFormat;
        Rules = matchRules;
        OriginDate = date;

        Home = new(homeTeam);
        Away = new(awayTeam);
    }

    public static Match Create(DateTime date, TeamReference homeTeam, TeamReference awayTeam, MatchFormat? matchFormat = null, MatchRules? matchRules = null)
        => new(MatchId.New(), date, homeTeam, awayTeam, matchFormat ?? MatchFormat.Default, matchRules ?? MatchRules.Default);

    public virtual MatchFormat Format { get; }

    public virtual MatchRules Rules { get; }

    public DateTime OriginDate { get; set; }

    public DateTime? PostponedDate { get; set; }

    public DateTime Date => PostponedDate ?? OriginDate;

    public MatchStatus Status { get; private set; }

    public MatchOpponent Home { get; private set; }

    public MatchOpponent Away { get; private set; }

    public TeamReference HomeTeamReference => Home.Team;

    public TeamReference AwayTeamReference => Away.Team;

    public bool IsNeutralStadium { get; set; }

    public StadiumId? StadiumId { get; set; }

    public bool AfterExtraTime { get; set; }

    public virtual bool UseExtraTime() => Format.ExtraTimeIsEnabled && IsDraw();

    public virtual bool UseShootout() => Format.ShootoutIsEnabled && IsDraw();

    public void Schedule(DateTime date)
    {
        if (Status == MatchStatus.Postponed && PostponedDate.HasValue)
            PostponedDate = date;
        else
            OriginDate = date;
    }

    public void Schedule(int offset, TimeUnit timeUnit)
    {
        if (offset == 0) return;

        if (Status == MatchStatus.Postponed && PostponedDate.HasValue)
            PostponedDate = PostponedDate.Value.AddFluentTimeSpan(offset.Unit(timeUnit));
        else
            OriginDate = OriginDate.AddFluentTimeSpan(offset.Unit(timeUnit));
    }

    public void Reset() => Reset(MatchStatus.None);

    public void Cancel() => Reset(MatchStatus.Cancelled);

    public void Postpone(DateTime? date = null)
    {
        Reset(MatchStatus.Postponed);
        PostponedDate = date;
    }

    public void Start() => Status = MatchStatus.InProgress;

    public void Suspend() => Status = MatchStatus.Suspended;

    public void Played() => Status = MatchStatus.Played;

    private void Reset(MatchStatus status)
    {
        Home?.Reset();
        Away?.Reset();
        AfterExtraTime = false;
        Status = status;
    }

    public Period GetPeriod() => new(Date, Date.AddFluentTimeSpan(Format.GetFullTime()));

    public void Invert() => (Home, Away) = (Away, Home);

    public bool HasResult() => Home is not null && Away is not null && Status is MatchStatus.Played or MatchStatus.InProgress or MatchStatus.Suspended;

    public bool HasResult(TeamReference team) => HasResult() && Participate(team);

    public bool IsPlayed() => Status == MatchStatus.Played;

    public bool IsDraw() => Home is not null && Away is not null && Home.GetScore() == Away.GetScore();

    public MatchResultType GetResultOf(TeamReference team)
        => !HasResult(team)
            ? MatchResultType.None
            : GetOpponent(team)!.IsWithdrawn
            ? MatchResultType.Withdraw
            : GetOpponentAgainst(team)!.IsWithdrawn
            ? MatchResultType.Win
            : GetResultTypeOf(team, true);

    public MatchOutcome GetOutcomeOf(TeamReference team)
        => GetResultOf(team) switch
        {
            MatchResultType.Win or MatchResultType.WinAfterShootouts => MatchOutcome.Win,
            MatchResultType.Draw => MatchOutcome.Draw,
            MatchResultType.Loss or MatchResultType.Withdraw or MatchResultType.LossAfterShootouts => MatchOutcome.Loss,
            MatchResultType.None => MatchOutcome.None,
            _ => throw new InvalidOperationException()
        };

    private MatchResultType GetResultTypeOf(TeamReference team, bool withShootout = true)
        => !HasResult(team)
            ? MatchResultType.None
            : GoalsFor(team) > GoalsAgainst(team) ? MatchResultType.Win
            : GoalsAgainst(team) > GoalsFor(team) ? MatchResultType.Loss
            : Format.ShootoutIsEnabled && withShootout ? GetShootoutResultOf(team) : MatchResultType.Draw;

    private MatchResultType GetShootoutResultOf(TeamReference team)
        => !HasResult(team)
            ? MatchResultType.None
            : ShootoutFor(team) > ShootoutAgainst(team) ? MatchResultType.WinAfterShootouts
            : ShootoutAgainst(team) > ShootoutFor(team) ? MatchResultType.LossAfterShootouts
            : MatchResultType.Draw;

    public TeamReference? GetWinner()
        => Home is null || Away is null
            ? null
            : GetOutcomeOf(Home.Team) switch
            {
                MatchOutcome.Win => Home.Team,
                MatchOutcome.Loss => Away.Team,
                MatchOutcome.None => null,
                MatchOutcome.Draw => null,
                _ => throw new InvalidOperationException()
            };

    public TeamReference? GetLooser()
        => Home is null || Away is null
            ? null
            : GetOutcomeOf(Home.Team) switch
            {
                MatchOutcome.Win => Away.Team,
                MatchOutcome.Loss => Home.Team,
                MatchOutcome.None => null,
                MatchOutcome.Draw => null,
                _ => throw new InvalidOperationException()
            };

    public bool IsWonBy(TeamReference team) => GetOutcomeOf(team) == MatchOutcome.Win;

    public bool IsLostBy(TeamReference team) => GetOutcomeOf(team) == MatchOutcome.Loss;

    public bool IsWithdrawn(TeamReference team) => GetOpponent(team)?.IsWithdrawn ?? false;

    public int GoalsFor(TeamReference team) => GetOpponent(team)?.GetScore() ?? 0;

    public int GoalsAgainst(TeamReference team) => GetOpponentAgainst(team)?.GetScore() ?? 0;

    public int ShootoutFor(TeamReference team) => GetOpponent(team)?.GetShootoutScore() ?? 0;

    public int ShootoutAgainst(TeamReference team) => GetOpponentAgainst(team)?.GetShootoutScore() ?? 0;

    public bool Participate(TeamReference team) => GetTeams().Contains(team);

    public bool IsHomeTeam(TeamReference team) => team == Home.Team;

    public bool IsAwayTeam(TeamReference team) => team == Away.Team;

    public MatchOpponent? GetOpponent(TeamReference team) => IsHomeTeam(team) ? Home : IsAwayTeam(team) ? Away : null;

    public MatchOpponent? GetOpponentAgainst(TeamReference team) => IsHomeTeam(team) ? Away : IsAwayTeam(team) ? Home : null;

    public IReadOnlyCollection<TeamReference> GetTeams() => new List<TeamReference>() { Home.Team, Away.Team }.AsReadOnly();

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
        var str = new StringBuilder($"{Date:G} | {Home.Team} vs {Away.Team}");

        if (Home is not null && Away is not null && (Status == MatchStatus.Played || Status == MatchStatus.InProgress || Status == MatchStatus.Suspended))
        {
            if (Home.IsWithdrawn)
            {
                _ = str.Append(CultureInfo.CurrentCulture, $"{Home.Team} is withdrawn");
            }
            else if (Away.IsWithdrawn)
            {
                _ = str.Append(CultureInfo.CurrentCulture, $"{Away.Team} is withdrawn");
            }
            else
            {
                _ = str.Append(CultureInfo.CurrentCulture, $" : {Home.GetScore()}-{Away.GetScore()}");

                if (Home.GetScore() == Away.GetScore())
                {
                    if (Format.ShootoutIsEnabled)
                    {
                        _ = str.Append(CultureInfo.CurrentCulture, $" ({Home.GetShootoutScore()}-{Away.GetShootoutScore()})");
                    }
                }
                else if (AfterExtraTime && Format.ExtraTimeIsEnabled)
                {
                    _ = str.Append(" e");
                }
            }
        }

        return str.ToString();
    }
}
