﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyNet.Utilities.Helpers;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Domain;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.MatchAggregate
{
    public class MatchOpponent : Entity
    {
        private readonly ObservableCollection<MatchEvent> _events = [];
        private readonly ObservableCollection<PenaltyShootout> _shootout = [];

        public MatchOpponent(Team team)
        {
            Team = team;
            Events = new(_events);
            Shootout = new(_shootout);
        }

        public Team Team { get; }

        public bool IsWithdrawn { get; private set; }

        public int PenaltyPoints { get; private set; }

        public ReadOnlyObservableCollection<MatchEvent> Events { get; }

        public ReadOnlyObservableCollection<PenaltyShootout> Shootout { get; }

        public Goal AddGoal(int? minute = null) => AddGoal(new Goal(GoalType.Other, minute: minute));

        public Goal AddGoal(Goal goal)
        {
            _events.Add(goal);

            return goal;
        }

        public PenaltyShootout AddPenaltyShootout(PenaltyShootoutResult result, Player? taker = null) => AddPenaltyShootout(new PenaltyShootout(taker, result));

        public PenaltyShootout AddPenaltyShootout(PenaltyShootout penaltyShootout)
        {
            _shootout.Add(penaltyShootout);

            return penaltyShootout;
        }

        public IEnumerable<Goal> GetGoals() => Events.OfType<Goal>();

        public IEnumerable<Card> GetCards() => Events.OfType<Card>();

        public int GetScore() => GetGoals().ToList().Count;

        public void SetScore(int score, int? shootoutScore = null)
        {
            ResetScore();
            EnumerableHelper.Iteration(score, _ => AddGoal());

            if (shootoutScore.HasValue)
                EnumerableHelper.Iteration(shootoutScore.Value, _ => AddPenaltyShootout(new PenaltyShootout(result: PenaltyShootoutResult.Succeeded)));
        }

        public int GetShootoutScore() => Shootout.Count(x => x.Result == PenaltyShootoutResult.Succeeded);

        public Card AddCard(Card card)
        {
            _events.Add(card);

            return card;
        }

        public void DoWithdraw(int penaltyPoints = 0) => Reset(true, penaltyPoints);

        public void Reset() => Reset(false, 0);

        private void Reset(bool isWithdrawn, int penaltyPoints)
        {
            IsWithdrawn = isWithdrawn;
            PenaltyPoints = penaltyPoints;
            _events.Clear();
        }

        private void ResetScore()
        {
            GetGoals().ToList().ForEach(x => _events.Remove(x));
            _shootout.Clear();
        }
    }
}
