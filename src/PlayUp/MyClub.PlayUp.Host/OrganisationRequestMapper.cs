// -----------------------------------------------------------------------
// <copyright file="OrganisationRequestMapper.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Host.Contracts;

namespace MyClub.PlayUp.Host;

/// <summary>
/// Maps Organisation HTTP contracts to Application commands (no business logic).
/// </summary>
public static class OrganisationRequestMapper
{
    /// <summary>
    /// Maps <see cref="ConfigureStructureRequest"/> to <see cref="StructureIntent"/>.
    /// </summary>
    public static StructureIntent ToStructureIntent(ConfigureStructureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var format = request.Format.Trim();
        var matchGenerationFormat = ParseMatchGenerationFormat(request.MatchGenerationFormat);

        return format.Equals("Championship", StringComparison.OrdinalIgnoreCase)
            || format.Equals("Championnat", StringComparison.OrdinalIgnoreCase)
            ? StructureIntent.Championship(request.MatchdayCount ?? 1, request.StageName, matchGenerationFormat)
            : format.Equals("Groups", StringComparison.OrdinalIgnoreCase)
            || format.Equals("Groupes", StringComparison.OrdinalIgnoreCase)
            ? request.GroupCount is null || request.ParticipantsPerGroup is null
                ? throw new ApplicationFailureException(
                    "Groups format requires GroupCount and ParticipantsPerGroup.",
                    ApplicationErrorCodes.InvalidStructureIntent)
                : StructureIntent.Groups(
                    request.GroupCount.Value,
                    request.ParticipantsPerGroup.Value,
                    request.StageName,
                    matchGenerationFormat)
            : format.Equals("Cup", StringComparison.OrdinalIgnoreCase)
            || format.Equals("Coupe", StringComparison.OrdinalIgnoreCase)
            ? request.BracketSize is null
                ? throw new ApplicationFailureException(
                    "Cup format requires BracketSize (power of two, 2–64).",
                    ApplicationErrorCodes.InvalidStructureIntent)
                : StructureIntent.Cup(request.BracketSize.Value, request.StageName)
            : format.Equals("Swiss", StringComparison.OrdinalIgnoreCase)
            ? request.SwissRoundCount is null
                ? throw new ApplicationFailureException(
                    "Swiss format requires SwissRoundCount (≥ 1).",
                    ApplicationErrorCodes.InvalidStructureIntent)
                : StructureIntent.Swiss(request.SwissRoundCount.Value, request.StageName)
            : throw new ApplicationFailureException(
            $"Unknown organisation format '{request.Format}'. Expected Championship, Groups, Cup, or Swiss.",
            ApplicationErrorCodes.InvalidStructureIntent);
    }

    /// <summary>
    /// Maps <see cref="ReplaceRegulationRequest"/> to Domain <see cref="Regulation"/>.
    /// </summary>
    /// <param name="request">HTTP replace body.</param>
    /// <param name="existingDisciplinaryRules">
    /// Current competition disciplinary rules — used when <see cref="ReplaceRegulationRequest.AllowedTypes"/> is omitted.
    /// </param>
    public static Regulation ToRegulation(
        ReplaceRegulationRequest request,
        DisciplinaryRules existingDisciplinaryRules)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(existingDisciplinaryRules);

        var disciplinary = request.AllowedTypes is null
            ? existingDisciplinaryRules
            : new DisciplinaryRules(request.AllowedTypes);

        ExtraTimePolicy? extraTime = null;
        if (request.HasExtraTime)
        {
            if (request.ExtraTimeDurationPerPeriod is null || request.ExtraTimeNumberOfPeriods is null)
            {
                throw new ApplicationFailureException(
                    "HasExtraTime requires ExtraTimeDurationPerPeriod and ExtraTimeNumberOfPeriods.",
                    ApplicationErrorCodes.InvalidStructureIntent);
            }

            extraTime = new ExtraTimePolicy(
                request.ExtraTimeDurationPerPeriod.Value,
                request.ExtraTimeNumberOfPeriods.Value);
        }

        PenaltyShootoutPolicy? shootout = null;
        if (request.HasPenaltyShootout)
        {
            if (request.PenaltyInitialKicksPerTeam is null)
            {
                throw new ApplicationFailureException(
                    "HasPenaltyShootout requires PenaltyInitialKicksPerTeam.",
                    ApplicationErrorCodes.InvalidStructureIntent);
            }

            shootout = new PenaltyShootoutPolicy(request.PenaltyInitialKicksPerTeam.Value);
        }

        var criteria = request.RankingCriteria is { Count: > 0 }
            ? request.RankingCriteria
            :
            [
                RankingCriterion.Points,
                RankingCriterion.GoalDifference,
                RankingCriterion.GoalsFor,
                RankingCriterion.HeadToHead
            ];

        return new Regulation(
            new EntryRules(request.MinimumTeams, request.MaximumTeams),
            new MatchRules(
                new MatchDuration(
                    request.DurationPerPeriod,
                    request.NumberOfPeriods,
                    request.HalfTimeDuration),
                new AdministrativeResultPolicy(request.ForfeitWinnerGoals, request.ForfeitLoserGoals),
                extraTime,
                shootout),
            new StandingRules(
                new PointsPolicy(request.WinPoints, request.DrawPoints, request.LossPoints),
                criteria),
            disciplinary);
    }

    /// <summary>
    /// Maps <see cref="ReplaceStageMatchRulesRequest"/> to <see cref="MatchRules"/>.
    /// </summary>
    public static MatchRules ToMatchRules(ReplaceStageMatchRulesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ExtraTimePolicy? extraTime = null;
        if (request.HasExtraTime)
        {
            if (request.ExtraTimeDurationPerPeriod is null || request.ExtraTimeNumberOfPeriods is null)
            {
                throw new ApplicationFailureException(
                    "HasExtraTime requires ExtraTimeDurationPerPeriod and ExtraTimeNumberOfPeriods.",
                    ApplicationErrorCodes.InvalidStructureIntent);
            }

            extraTime = new ExtraTimePolicy(
                request.ExtraTimeDurationPerPeriod.Value,
                request.ExtraTimeNumberOfPeriods.Value);
        }

        PenaltyShootoutPolicy? shootout = null;
        if (request.HasPenaltyShootout)
        {
            if (request.PenaltyInitialKicksPerTeam is null)
            {
                throw new ApplicationFailureException(
                    "HasPenaltyShootout requires PenaltyInitialKicksPerTeam.",
                    ApplicationErrorCodes.InvalidStructureIntent);
            }

            shootout = new PenaltyShootoutPolicy(request.PenaltyInitialKicksPerTeam.Value);
        }

        return new MatchRules(
            new MatchDuration(
                request.DurationPerPeriod,
                request.NumberOfPeriods,
                request.HalfTimeDuration),
            new AdministrativeResultPolicy(request.ForfeitWinnerGoals, request.ForfeitLoserGoals),
            extraTime,
            shootout);
    }

    private static MatchGenerationFormat ParseMatchGenerationFormat(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? MatchGenerationFormat.SingleRoundRobin
            : Enum.TryParse<MatchGenerationFormat>(value.Trim(), ignoreCase: true, out var parsed)
              && Enum.IsDefined(parsed)
                ? parsed
                : throw new ApplicationFailureException(
                    $"Unknown MatchGenerationFormat '{value}'. Expected SingleRoundRobin or DoubleRoundRobin.",
                    ApplicationErrorCodes.InvalidStructureIntent);
}
