// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class GoalViewModel : EntityViewModelBase<Goal>, IMatchEventViewModel
    {
        private readonly TeamViewModel _team;

        public GoalViewModel(Goal item, TeamViewModel team) : base(item) => _team = team;

        public int? Minute => Item.Minute;

        public GoalType Type => Item.Type;

        public PlayerViewModel? Scorer => Item.Scorer is not null ? _team.Players.GetById(Item.Scorer.Id) : null;

        public PlayerViewModel? Assist => Item.Assist is not null ? _team.Players.GetById(Item.Assist.Id) : null;

        public string? DisplayName => Scorer?.FullName;
    }
}
