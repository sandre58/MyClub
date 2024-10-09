// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class PenaltyShootoutViewModel : EntityViewModelBase<PenaltyShootout>
    {
        private readonly TeamViewModel _team;

        public PenaltyShootoutViewModel(PenaltyShootout item, TeamViewModel team) : base(item) => _team = team;

        public PenaltyShootoutResult Result => Item.Result;

        public PlayerViewModel? Taker => Item.Taker is not null ? _team.Players.GetById(Item.Taker.Id) : null;
    }
}
