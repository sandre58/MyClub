// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class CardViewModel : EntityViewModelBase<Card>, IMatchEventViewModel
    {
        private readonly TeamViewModel _team;

        public CardViewModel(Card item, TeamViewModel team) : base(item) => _team = team;

        public int? Minute => Item.Minute;

        public CardColor Color => Item.Color;

        public CardInfraction Infraction => Item.Infraction;

        public string? Description => Item.Description;

        public PlayerViewModel? Player => Item.Player is not null ? _team.Players.GetById(Item.Player.Id) : null;

        public string? DisplayName => Player?.FullName;
    }
}
