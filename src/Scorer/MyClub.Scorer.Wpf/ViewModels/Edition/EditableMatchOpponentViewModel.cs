// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.UI.Resources;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableMatchOpponentViewModel : EditableObject
    {
        public EditableMatchOpponentViewModel(string teamResourceKey)
        {
            ValidationRules.Add<EditableMatchOpponentViewModel, IVirtualTeamViewModel?>(x => x.Team, () => MessageResources.FieldXIsRequiredError.FormatWith(teamResourceKey.Translate()), x => x is not null);

            RemoveGoalCommand = CommandsManager.CreateNotNull<EditableGoalViewModel>(RemoveGoal, x => x.Type.HasValue);
            AddGoalCommand = CommandsManager.Create(AddGoal);
            RemovePenaltyShootoutCommand = CommandsManager.CreateNotNull<EditablePenaltyShootoutViewModel>(RemovePenaltyShootout);
            AddNonePenaltyShootoutCommand = CommandsManager.Create<PlayerViewModel>(x => AddPenaltyShootout(x, PenaltyShootoutResult.None));
            AddFailedPenaltyShootoutCommand = CommandsManager.Create<PlayerViewModel>(x => AddPenaltyShootout(x, PenaltyShootoutResult.Failed));
            AddSucceededPenaltyShootoutCommand = CommandsManager.Create<PlayerViewModel>(x => AddPenaltyShootout(x, PenaltyShootoutResult.Succeeded));
            AddCardCommand = CommandsManager.CreateNotNull<CardColor>(AddCard);
            RemoveCardCommand = CommandsManager.CreateNotNull<EditableCardViewModel>(RemoveCard, x => x.Color.HasValue);

            Disposables.AddRange(
            [
                this.WhenPropertyChanged(x => x.NewGoalIsEditing, false).Subscribe(x =>
                {
                    if(!x.Value && NewGoal.Type.HasValue)
                    {
                        Goals.Add(NewGoal);
                        NewGoal = new() { Type = null };
                    }
                }),
                this.WhenPropertyChanged(x => x.NewCardIsEditing, false).Subscribe(x =>
                {
                    if(!x.Value && NewCard.Color.HasValue)
                    {
                        Cards.Add(NewCard);
                        NewCard = new() { Color = null };
                    }
                }),
                this.WhenPropertyChanged(x => x.Team, false).Subscribe(_ => ResetInternal(false)),
                Shootout.ToObservableChangeSet().AutoRefresh(x => x.Result).Filter(x => x.Result == PenaltyShootoutResult.Succeeded).Count().Subscribe(x => SucceedShootouts = x)
            ]);
        }

        public IVirtualTeamViewModel? Team { get; set; }

        public TeamViewModel? ComputedTeam { get; private set; }

        [Display(Name = nameof(IsWithdrawn), ResourceType = typeof(MyClubResources))]
        public bool IsWithdrawn { get; set; }

        public UiObservableCollection<EditableGoalViewModel> Goals { get; } = [];

        public UiObservableCollection<EditablePenaltyShootoutViewModel> Shootout { get; } = [];

        public UiObservableCollection<EditableCardViewModel> Cards { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public EditableGoalViewModel NewGoal { get; private set; } = new() { Type = null };

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool NewGoalIsEditing { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public EditableCardViewModel NewCard { get; private set; } = new() { Color = null };

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool NewCardIsEditing { get; set; }

        public int SucceedShootouts { get; private set; }

        public ICommand AddGoalCommand { get; }

        public ICommand RemoveGoalCommand { get; }

        public ICommand AddNonePenaltyShootoutCommand { get; }

        public ICommand AddFailedPenaltyShootoutCommand { get; }

        public ICommand AddSucceededPenaltyShootoutCommand { get; }

        public ICommand RemovePenaltyShootoutCommand { get; }

        public ICommand AddCardCommand { get; }

        public ICommand RemoveCardCommand { get; }

        public void AddGoal() => Goals.Add(new EditableGoalViewModel());

        private void RemoveGoal(EditableGoalViewModel goal)
        {
            if (!Goals.Remove(goal))
                Goals.Remove(goal);
        }

        public void AddPenaltyShootout(PlayerViewModel? player, PenaltyShootoutResult result) => Shootout.Add(new EditablePenaltyShootoutViewModel() { Taker = player, Result = result });

        private void RemovePenaltyShootout(EditablePenaltyShootoutViewModel penalty)
        {
            if (!Shootout.Remove(penalty))
                Shootout.Remove(penalty);
        }

        public void AddCard(CardColor color) => Cards.Add(new EditableCardViewModel() { Color = color });

        private void RemoveCard(EditableCardViewModel card)
        {
            if (!Cards.Remove(card))
                Cards.Remove(card);
        }

        public void ResetScore()
        {
            if (Goals.Count > 0)
                Goals.Clear();
            if (Shootout.Count > 0)
                Shootout.Clear();
            IsWithdrawn = false;
        }

        public void Reset() => ResetInternal(true);

        public void ResetInternal(bool withTeam)
        {
            if (withTeam)
                Team = null;
            ComputeTeam();
            ResetScore();
            if (Cards.Count > 0)
                Cards.Clear();
        }

        public void Load(IVirtualTeamViewModel team, MatchState state, MatchOpponent? opponent)
        {
            var hasScore = state is MatchState.InProgress or MatchState.Suspended or MatchState.Played;
            Team = team;
            ComputeTeam();
            IsWithdrawn = opponent?.IsWithdrawn ?? false;
            Goals.Set(hasScore ? opponent?.GetGoals().Select(x => new EditableGoalViewModel
            {
                Type = x.Type,
                Assist = x.Assist is not null ? ComputedTeam?.Players.GetByIdOrDefault(x.Assist.Id) : null,
                Scorer = x.Scorer is not null ? ComputedTeam?.Players.GetByIdOrDefault(x.Scorer.Id) : null,
                Minute = x.Minute ?? 1,
                MinuteIsEnabled = x.Minute.HasValue
            }).OrderBy(x => x.Minute) : []);
            Shootout.Set(hasScore ? opponent?.Shootout.Select(x => new EditablePenaltyShootoutViewModel
            {
                Result = x.Result,
                Taker = x.Taker is not null ? ComputedTeam?.Players.GetByIdOrDefault(x.Taker.Id) : null,
            }) : []);
            Cards.Set(hasScore ? opponent?.GetCards().Select(x => new EditableCardViewModel
            {
                Color = x.Color,
                Infraction = x.Infraction,
                Description = x.Description,
                Player = x.Player is not null ? ComputedTeam?.Players.GetByIdOrDefault(x.Player.Id) : null,
                Minute = x.Minute ?? 1,
                MinuteIsEnabled = x.Minute.HasValue
            }) : []);
        }

        internal void DoWithdraw()
        {
            IsWithdrawn = true;
            Goals.Clear();
            Shootout.Clear();
        }
        internal void WinByWithdraw()
        {
            ResetScore();
            3.Iteration(_ => AddGoal());
        }

        private void ComputeTeam()
            => ComputedTeam = Team is null
                ? null
                : Team is TeamViewModel team
                ? team
                : Team.ProvideTeam();
    }
}
