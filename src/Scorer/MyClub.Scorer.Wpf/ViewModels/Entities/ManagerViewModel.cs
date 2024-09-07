﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Wpf.Services;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class ManagerViewModel : PersonViewModel
    {
        private readonly PersonPresentationService _personPresentationService;

        public ManagerViewModel(Manager item, TeamViewModel team, PersonPresentationService personPresentationService) : base(item)
        {
            _personPresentationService = personPresentationService;

            Team = team;
        }

        public Guid TeamId => Team.Id;

        public TeamViewModel Team { get; }

        public override async Task OpenAsync() => await _personPresentationService.OpenAsync(this).ConfigureAwait(false);

        public override async Task EditAsync() => await _personPresentationService.EditAsync(this).ConfigureAwait(false);
    }
}