// -----------------------------------------------------------------------
// <copyright file="TeamMappingProfile.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using AutoMapper;
using MyClub.Scorer.Application.Competitions.Commands.AddTeam;
using MyClub.Scorer.Application.Competitions.Commands.UpdateTeam;
using MyClub.Scorer.Application.Competitions.Queries.GetTeams.Dtos;
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

        // Team → TeamDto (full mapping)
        _ = CreateMap<Team, TeamDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.DisplayName.Name))
            .ForMember(dest => dest.ShortName, opt => opt.MapFrom(src => src.DisplayName.ShortName))
            .ForMember(dest => dest.Logo, opt => opt.MapFrom(src => src.Logo))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country))
            .ForMember(dest => dest.StadiumId, opt => opt.MapFrom(src => src.StadiumId))
            .ForMember(dest => dest.HomeColor, opt => opt.MapFrom(src => src.HomeColor))
            .ForMember(dest => dest.AwayColor, opt => opt.MapFrom(src => src.AwayColor))
            .ForMember(dest => dest.Players, opt => opt.MapFrom(src => src.Players))
            .ForMember(dest => dest.Staff, opt => opt.MapFrom(src => src.Staff));

        // Player → PlayerDto
        _ = CreateMap<Player, PlayerDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country))
            .ForMember(dest => dest.Photo, opt => opt.MapFrom(src => src.Photo))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.LicenseNumber, opt => opt.MapFrom(src => src.LicenseNumber))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));

        // Manager → ManagerDto
        _ = CreateMap<Manager, ManagerDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country))
            .ForMember(dest => dest.Photo, opt => opt.MapFrom(src => src.Photo))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.LicenseNumber, opt => opt.MapFrom(src => src.LicenseNumber))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));
    }
}
