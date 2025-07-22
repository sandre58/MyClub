// -----------------------------------------------------------------------
// <copyright file="MatchOpponent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Scorer.Domain.MatchAggregate.MatchEvents;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Domain.MatchAggregate;

public class MatchOpponent(TeamReference team)
{
    private readonly List<MatchEvent> _events = [];
    private readonly List<PenaltyShootout> _shootout = [];

    public TeamReference Team { get; } = team;

    public bool IsWithdrawn { get; private set; }

    public IReadOnlyCollection<MatchEvent> Events => _events.AsReadOnly();

    public IReadOnlyCollection<PenaltyShootout> Shootout => _shootout.AsReadOnly();

    public Goal AddGoal(int? minute = null) => AddGoal(Goal.Create(GoalType.Regular, minute: minute));

    public Goal AddGoal(Goal goal)
    {
        _events.Add(goal);

        return goal;
    }

    public void RemoveLastGoal()
    {
        var lastGoal = _events.OfType<Goal>().LastOrDefault();

        if (lastGoal is not null)
            _ = _events.Remove(lastGoal);
    }

    public PenaltyShootout AddPenaltyShootout(PenaltyShootoutOutcome result, PlayerId? takerId = null) => AddPenaltyShootout(PenaltyShootout.Create(takerId, result));

    public PenaltyShootout AddPenaltyShootout(PenaltyShootout penaltyShootout)
    {
        _shootout.Add(penaltyShootout);

        return penaltyShootout;
    }

    public void RemoveLastSucceededPenaltyShootout()
    {
        var lastPenaltyShootout = _shootout.LastOrDefault(x => x.Result == PenaltyShootoutOutcome.Succeeded);

        if (lastPenaltyShootout is not null)
            _ = _shootout.Remove(lastPenaltyShootout);
    }

    public void SetCards(IEnumerable<Card> cards)
    {
        GetCards().ToList().ForEach(x => _events.Remove(x));
        cards.ForEach(x => AddCard(x));
    }

    public IEnumerable<Goal> GetGoals() => Events.OfType<Goal>();

    public IEnumerable<Card> GetCards() => Events.OfType<Card>();

    public int GetScore() => GetGoals().ToList().Count;

    public void SetScore(int score, int? shootoutScore = null)
    {
        ResetScore();
        EnumerableHelper.Iteration(score, _ => AddGoal());

        if (shootoutScore.HasValue)
            EnumerableHelper.Iteration(shootoutScore.Value, _ => AddPenaltyShootout(PenaltyShootout.Create(result: PenaltyShootoutOutcome.Succeeded)));
    }

    public void SetScore(IEnumerable<Goal> goals, IEnumerable<PenaltyShootout>? shootouts = null)
    {
        ResetScore();
        goals.ForEach(x => AddGoal(x));
        shootouts?.ForEach(x => AddPenaltyShootout(x));
    }

    public int GetShootoutScore() => Shootout.Count(x => x.Result == PenaltyShootoutOutcome.Succeeded);

    public Card AddCard(Card card)
    {
        _events.Add(card);

        return card;
    }

    public void DoWithdraw() => Reset(true);

    public void Reset() => Reset(false);

    private void Reset(bool isWithdrawn)
    {
        IsWithdrawn = isWithdrawn;
        _events.Clear();
        _shootout.Clear();
    }

    private void ResetScore()
    {
        GetGoals().ToList().ForEach(x => _events.Remove(x));
        _shootout.Clear();
    }

    public override string? ToString() => Team.ToString();
}
