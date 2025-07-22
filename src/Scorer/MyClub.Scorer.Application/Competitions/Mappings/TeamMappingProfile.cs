// -----------------------------------------------------------------------
// <copyright file="TeamMappingProfile.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using AutoMapper;
using MyClub.Scorer.Application.Competitions.Commands.AddTeam;
using MyClub.Scorer.Application.Competitions.Commands.UpdateTeam;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Shared.Domain.ValueObjects;

namespace MyClub.Scorer.Application.Competitions.Mappings;

public class TeamMappingProfile : Profile
{
    public TeamMappingProfile()
    {
        // AddTeamCommand → Team
        _ = CreateMap<AddTeamCommand, Team>()
                .ConstructUsing(cmd => Team.Create(cmd.Name, cmd.ShortName));

        // UpdateTeamCommand → Team (mise à jour des propriétés)
        _ = CreateMap<UpdateTeamCommand, Team>()
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => new DisplayName(src.Name, src.ShortName)));
    }
}
