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

/// <summary>
/// AutoMapper profile for mapping between team-related commands and domain entities.
/// This profile defines the transformation rules for converting application layer
/// data transfer objects into rich domain entities and vice versa.
/// </summary>
public class TeamMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeamMappingProfile"/> class.
    /// </summary>
    public TeamMappingProfile()
    {
        // AddTeamCommand → Team
        // Uses the Team.Create factory method to ensure proper domain entity construction
        _ = CreateMap<AddTeamCommand, Team>()
                .ConstructUsing(static cmd => Team.Create(cmd.Name, cmd.ShortName));

        // UpdateTeamCommand → Team
        // Maps command properties to domain entity, constructing value objects as needed
        _ = CreateMap<UpdateTeamCommand, Team>()
                .ForMember(static dest => dest.DisplayName, static opt => opt.MapFrom(static src => new DisplayName(src.Name, src.ShortName)));
    }
}
