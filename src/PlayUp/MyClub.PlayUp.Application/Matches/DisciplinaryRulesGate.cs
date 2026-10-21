// -----------------------------------------------------------------------
// <copyright file="DisciplinaryRulesGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application gate for competition disciplinary catalogue authorization (D1 / cases 2 &amp; 4).
/// </summary>
internal static class DisciplinaryRulesGate
{
    public static void EnsureAllowed(Match match, Competition competition, DisciplinaryType type)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(competition);
        EnsureDefinedType(type);

        if (!match.CompetitionId.Equals(competition.Id))
        {
            throw new ApplicationFailureException(
                $"Match '{match.Id}' does not belong to competition '{competition.Id}'.",
                ApplicationErrorCodes.MatchNotInCompetition);
        }

        if (!competition.Regulation.DisciplinaryRules.Allows(type))
        {
            throw new ApplicationFailureException(
                $"Disciplinary type '{type}' is not allowed by competition rules.",
                ApplicationErrorCodes.DisciplinaryTypeNotAllowed);
        }
    }

    internal static void EnsureDefinedType(DisciplinaryType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ApplicationFailureException(
                $"Unknown disciplinary type '{type}'.",
                ApplicationErrorCodes.InvalidDisciplinaryType);
        }
    }
}
