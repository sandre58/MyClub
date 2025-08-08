// -----------------------------------------------------------------------
// <copyright file="AddTeamCommandValidator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace MyClub.Scorer.Application.Competitions.Commands.AddTeam;

/// <summary>
/// Provides comprehensive validation rules for the AddTeamCommand using FluentValidation.
/// This validator ensures data integrity and enforces business constraints before
/// the command reaches the domain layer for execution.
/// </summary>
public sealed class AddTeamCommandValidator : AbstractValidator<AddTeamCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddTeamCommandValidator"/> class.
    /// </summary>
    public AddTeamCommandValidator()
    {
        // Competition ID is required and must be a valid GUID
        _ = RuleFor(static x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("CompetitionId is required.");

        // Team name is required and must be within reasonable length limits
        _ = RuleFor(static x => x.Name)
            .NotEmpty()
            .WithMessage("Team name is required.")
            .MaximumLength(100)
            .WithMessage("Team name must be at most 100 characters.");

        // Short name is optional but must be within display limits when provided
        _ = RuleFor(static x => x.ShortName)
            .MaximumLength(10)
            .WithMessage("Short name must be at most 10 characters.")
            .When(static x => !string.IsNullOrEmpty(x.ShortName));

        // Stadium ID must be a valid GUID when provided (optional field)
        _ = RuleFor(static x => x.StadiumId)
            .NotEmpty()
            .WithMessage("Stadium ID must be valid when specified.")
            .When(static x => x.StadiumId.HasValue);

        // Logo validation is handled at infrastructure level for size/format constraints
        // No validation needed here as it's pure binary data
    }
}
