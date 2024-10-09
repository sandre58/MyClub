// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyNet.Utilities.Helpers;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities.Collections;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class MatchOpponent : Entity
    {
        private readonly ExtendedObservableCollection<MatchEvent> _events = [];
        private readonly ExtendedObservableCollection<PenaltyShootout> _shootout = [];

        public MatchOpponent(Team team)
        {
            Team = team;
            Events = new(_events);
            Shootout = new(_shootout);
        }

        public Team Team { get; }

        public bool IsWithdrawn { get; private set; }

        public ReadOnlyObservableCollection<MatchEvent> Events { get; }

        public ReadOnlyObservableCollection<PenaltyShootout> Shootout { get; }

        public Goal AddGoal(int? minute = null) => AddGoal(new Goal(GoalType.Other, minute: minute));

        public Goal AddGoal(Goal goal)
        {
            _events.Add(goal);

            return goal;
        }

        public void RemoveLastGoal()
        {
            var lastGoal = _events.OfType<Goal>().LastOrDefault();

            if (lastGoal is not null)
                _events.Remove(lastGoal);
        }

        public PenaltyShootout AddPenaltyShootout(PenaltyShootoutResult result, Player? taker = null) => AddPenaltyShootout(new PenaltyShootout(taker, result));

        public PenaltyShootout AddPenaltyShootout(PenaltyShootout penaltyShootout)
        {
            _shootout.Add(penaltyShootout);

            return penaltyShootout;
        }

        public void RemoveLastSucceededPenaltyShootout()
        {
            var lastPenaltyShootout = _shootout.LastOrDefault(x => x.Result == PenaltyShootoutResult.Succeeded);

            if (lastPenaltyShootout is not null)
                _shootout.Remove(lastPenaltyShootout);
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
                EnumerableHelper.Iteration(shootoutScore.Value, _ => AddPenaltyShootout(new PenaltyShootout(result: PenaltyShootoutResult.Succeeded)));
        }

        public void SetScore(IEnumerable<Goal> goals, IEnumerable<PenaltyShootout>? shootouts = null)
        {
            ResetScore();
            goals.ForEach(x => AddGoal(x));
            shootouts?.ForEach(x => AddPenaltyShootout(x));
        }

        public int GetShootoutScore() => Shootout.Count(x => x.Result == PenaltyShootoutResult.Succeeded);

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
}
