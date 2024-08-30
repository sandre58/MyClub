// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class ManagerEditionViewModel : PersonEditionViewModel<Manager, ManagerDto, ManagerService>
    {
        public TeamViewModel? Team { get; private set; }

        public ManagerEditionViewModel(ManagerService managerService)
            : base(managerService)
        {
        }

        public void Load(TeamViewModel team, Guid id)
        {
            Team = team;
            Load(id);
        }

        public void New(TeamViewModel team, Action? initialize = null)
        {
            Team = team;
            New(initialize);
        }

        protected override ManagerDto ToDto()
        {
            var dto = base.ToDto();
            dto.TeamId = Team?.Id ?? Guid.Empty;

            return dto;
        }

        protected override void ResetItem()
        {
            var defaultValues = CrudService.New(Team?.Id ?? Guid.Empty);
            FirstName = defaultValues.FirstName.OrEmpty();
            LastName = defaultValues.LastName.OrEmpty();
            Photo = defaultValues.Photo;
            LicenseNumber = defaultValues.LicenseNumber;
            Gender = defaultValues.Gender;
            Country = defaultValues.Country;
        }
    }
}
