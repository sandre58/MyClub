// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyNet.Utilities;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class FriendlyEditionViewModel(CompetitionService service,
                                            TeamService teamService,
                                            TeamsProvider teamsProvider,
                                            TeamPresentationService teamPresentationService) : CompetitionEditionViewModel<Friendly, FriendlySeason, FriendlyDto>(service, teamService, teamsProvider, teamPresentationService)
    {
        protected override FriendlyDto CreateCompetitionDto() => new()
        {
            Rules = new FriendlyRules(MatchFormat.Create(), MatchTime)
        };

        protected override void RefreshFromCompetition(FriendlySeason season) { }

        protected override void ResetItem()
        {
            var defaultValues = CrudService.NewFriendly();
            StartDate = defaultValues.StartDate.Date;
            EndDate = defaultValues.EndDate.Date;
            Category = defaultValues.Category;
            Logo = defaultValues.Logo;
            Name = defaultValues.Name.OrEmpty();
            ShortName = defaultValues.ShortName.OrEmpty();
            Teams.Set(defaultValues.Teams is not null ? TeamSelectionViewModel.Source.Where(x => x.Id is not null && defaultValues.Teams.Select(y => y.Id).Contains(x.Id)).ToList() : Array.Empty<EditableTeamViewModel>());
            if (defaultValues.Rules is not null)
            {
                MatchTime = defaultValues.Rules.MatchTime;
                if (defaultValues.Rules.MatchFormat is not null)
                    MatchFormat.Load(defaultValues.Rules.MatchFormat);
                else
                    MatchFormat.Reset();
            }
        }
    }
}
