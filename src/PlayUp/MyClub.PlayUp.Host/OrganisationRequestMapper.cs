// -----------------------------------------------------------------------
// <copyright file="OrganisationRequestMapper.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Host.Contracts;

namespace MyClub.PlayUp.Host;

/// <summary>
/// Maps Organisation HTTP contracts to Application commands (no business logic).
/// </summary>
internal static class OrganisationRequestMapper
{
    /// <summary>
    /// Maps <see cref="ConfigureStructureRequest"/> to <see cref="StructureIntent"/>.
    /// </summary>
    public static StructureIntent ToStructureIntent(ConfigureStructureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var format = (request.Format ?? string.Empty).Trim();
        if (format.Equals("Championship", StringComparison.OrdinalIgnoreCase)
            || format.Equals("Championnat", StringComparison.OrdinalIgnoreCase))
        {
            return StructureIntent.Championship(request.MatchdayCount ?? 1, request.StageName);
        }

        if (format.Equals("Groups", StringComparison.OrdinalIgnoreCase)
            || format.Equals("Groupes", StringComparison.OrdinalIgnoreCase))
        {
            if (request.GroupCount is null || request.ParticipantsPerGroup is null)
            {
                throw new ApplicationFailureException(
                    "Groups format requires GroupCount and ParticipantsPerGroup.",
                    ApplicationErrorCodes.InvalidStructureIntent);
            }

            return StructureIntent.Groups(
                request.GroupCount.Value,
                request.ParticipantsPerGroup.Value,
                request.StageName);
        }

        if (format.Equals("Cup", StringComparison.OrdinalIgnoreCase)
            || format.Equals("Coupe", StringComparison.OrdinalIgnoreCase))
        {
            if (request.BracketSize is null)
            {
                throw new ApplicationFailureException(
                    "Cup format requires BracketSize (power of two, 2–64).",
                    ApplicationErrorCodes.InvalidStructureIntent);
            }

            return StructureIntent.Cup(request.BracketSize.Value, request.StageName);
        }

        throw new ApplicationFailureException(
            $"Unknown organisation format '{request.Format}'. Expected Championship, Groups, or Cup.",
            ApplicationErrorCodes.InvalidStructureIntent);
    }

    /// <summary>
    /// Maps <see cref="ReplaceRegulationRequest"/> to Domain <see cref="Regulation"/>.
    /// </summary>
    public static Regulation ToRegulation(ReplaceRegulationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new Regulation(
            new EntryRules(request.MinimumTeams, request.MaximumTeams),
            new MatchRules(
                new MatchDuration(
                    request.DurationPerPeriod,
                    request.NumberOfPeriods,
                    request.HalfTimeDuration),
                new AdministrativeResultPolicy(request.ForfeitWinnerGoals, request.ForfeitLoserGoals)),
            new StandingRules(
                new PointsPolicy(request.WinPoints, request.DrawPoints, request.LossPoints),
                [
                    RankingCriterion.Points,
                    RankingCriterion.GoalDifference,
                    RankingCriterion.GoalsFor,
                    RankingCriterion.HeadToHead
                ]));
    }
}
