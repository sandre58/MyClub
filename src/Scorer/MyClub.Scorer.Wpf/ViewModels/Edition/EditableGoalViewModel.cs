// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.UI.Commands;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableGoalViewModel : EditableObject
    {
        public EditableGoalViewModel()
        {
            ClearScorerCommand = CommandsManager.Create(() => Scorer = null, () => Scorer is not null);
            ClearAssistCommand = CommandsManager.Create(() => Assist = null, () => Assist is not null);
        }

        [Display(Name = nameof(Minute), ResourceType = typeof(MyClubResources))]
        public int Minute { get; set; } = 1;

        public bool MinuteIsEnabled { get; set; }

        [Display(Name = nameof(Type), ResourceType = typeof(MyClubResources))]
        public GoalType? Type { get; set; } = GoalType.Other;

        [Display(Name = nameof(Scorer), ResourceType = typeof(MyClubResources))]
        public PlayerViewModel? Scorer { get; set; }

        [Display(Name = nameof(Assist), ResourceType = typeof(MyClubResources))]
        public PlayerViewModel? Assist { get; set; }

        public ICommand ClearScorerCommand { get; }

        public ICommand ClearAssistCommand { get; }

        public GoalDto ToDto() => new()
        {
            ScorerId = Type != GoalType.OwnGoal ? Scorer?.Id : null,
            AssistId = Type == GoalType.Other && Scorer is not null ? Assist?.Id : null,
            Type = Type.GetValueOrDefault(),
            Minute = MinuteIsEnabled ? Minute : null,
        };

        protected virtual void OnTypeChanged()
        {
            if (Type != GoalType.Other)
                Assist = null;

            if (Type == GoalType.OwnGoal)
                Scorer = null;
        }

        protected virtual void OnScorerChanged() => Scorer.IfNull(() => Assist = null);
    }
}
