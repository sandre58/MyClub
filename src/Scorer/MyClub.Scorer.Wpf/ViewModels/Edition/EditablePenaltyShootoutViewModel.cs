// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.UI.Commands;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditablePenaltyShootoutViewModel : EditableObject
    {
        public EditablePenaltyShootoutViewModel()
        {
            SetResultToFailedCommand = CommandsManager.Create<PlayerViewModel>(x => SetResult(x, PenaltyShootoutResult.Failed), x => !Equals(Taker, x) || Result != PenaltyShootoutResult.Failed);
            SetResultToSucceededCommand = CommandsManager.Create<PlayerViewModel>(x => SetResult(x, PenaltyShootoutResult.Succeeded), x => !Equals(Taker, x) || Result != PenaltyShootoutResult.Succeeded);
            SetResultToNoneCommand = CommandsManager.Create<PlayerViewModel>(x => SetResult(x, PenaltyShootoutResult.None), x => !Equals(Taker, x) || Result != PenaltyShootoutResult.None);
        }

        [Display(Name = nameof(Result), ResourceType = typeof(MyClubResources))]
        public PenaltyShootoutResult? Result { get; set; } = PenaltyShootoutResult.None;

        [Display(Name = nameof(Taker), ResourceType = typeof(MyClubResources))]
        public PlayerViewModel? Taker { get; set; }

        public ICommand SetResultToNoneCommand { get; }

        public ICommand SetResultToFailedCommand { get; }

        public ICommand SetResultToSucceededCommand { get; }

        private void SetResult(PlayerViewModel? player, PenaltyShootoutResult result)
        {
            using (IsModifiedSuspender.Suspend())
                Taker = player;
            Result = result;
        }

        public void Reset() => Result = null;

        public PenaltyShootoutDto ToDto() => new()
        {
            TakerId = Taker?.Id,
            Result = Result.GetValueOrDefault(),
        };

        protected virtual void OnTakerChanged()
        {
            if (IsModifiedSuspender.IsSuspended) return;

            Result = PenaltyShootoutResult.None;
        }
    }
}
