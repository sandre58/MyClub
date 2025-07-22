// -----------------------------------------------------------------------
// <copyright file="AddTeamCommandValidator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace MyClub.Scorer.Application.Competitions.Commands.AddTeam;

public sealed class AddTeamCommandValidator : AbstractValidator<AddTeamCommand>
{
    public AddTeamCommandValidator()
    {
        _ = RuleFor(x => x.CompetitionId).NotEmpty().WithMessage("CompetitionId is required.");

        _ = RuleFor(x => x.Name).NotEmpty().WithMessage("Team name is required.")
                                .MaximumLength(100).WithMessage("Team name must be at most 100 characters.");
    }
}
