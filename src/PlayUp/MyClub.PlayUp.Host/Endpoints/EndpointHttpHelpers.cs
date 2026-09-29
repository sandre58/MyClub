// -----------------------------------------------------------------------
// <copyright file="EndpointHttpHelpers.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;

namespace MyClub.PlayUp.Host.Endpoints;

internal static class EndpointHttpHelpers
{
    internal static async Task<IResult> ConfigureStructureHttpAsync(
        Guid competitionId,
        ConfigureStructureRequest request,
        UseCaseExecutor executor,
        CancellationToken cancellationToken)
    {
        var intent = StructureRequestMapper.ToStructureIntent(request);
        var (result, view) = await executor
            .ConfigureStructureAsync(new CompetitionId(competitionId), intent, cancellationToken)
            .ConfigureAwait(false);
        var impact = result.RebuildImpact is null
            ? null
            : new StructureRebuildImpactDto(
                result.RebuildImpact.ClearedMatchdays,
                result.RebuildImpact.ClearedGroups,
                result.RebuildImpact.ClearedRounds,
                result.RebuildImpact.ClearedSlots,
                result.RebuildImpact.ClearedDirectAssignments,
                result.RebuildImpact.ClearedCompositionEntries,
                result.RebuildImpact.ClearedDrawRules,
                result.RebuildImpact.ClearedSwissSettings);
        return Results.Ok(new ConfigureStructureResponse(result.StageCreated, impact, view));
    }

    internal static CompletionMode ParseCompletionMode(string mode) => mode.Equals("Normal", StringComparison.OrdinalIgnoreCase)
        ? CompletionMode.Normal
        : mode.Equals("Administrative", StringComparison.OrdinalIgnoreCase)
            ? CompletionMode.Administrative
            : mode.Equals("Abandoned", StringComparison.OrdinalIgnoreCase)
                ? CompletionMode.Abandoned
                : throw new ApplicationFailureException(
                    $"Unknown completion mode '{mode}'. Expected Normal, Administrative, or Abandoned.",
                    ApplicationErrorCodes.InvalidCompletionMode);

    internal static DrawResolutionKind ParseDrawKind(string kind) => kind.Equals("Slot", StringComparison.OrdinalIgnoreCase)
        ? DrawResolutionKind.Slot
        : kind.Equals("Group", StringComparison.OrdinalIgnoreCase)
          || kind.Equals("Groups", StringComparison.OrdinalIgnoreCase)
            ? DrawResolutionKind.Group
            : throw new ApplicationFailureException(
                $"Unknown draw kind '{kind}'. Expected Slot or Group.",
                ApplicationErrorCodes.DrawKindNotSupported);

    internal static DrawInputsIntent ParseDrawInputsIntent(string? intent) => string.IsNullOrWhiteSpace(intent) || intent.Equals("Default", StringComparison.OrdinalIgnoreCase)
        ? DrawInputsIntent.Default
        : intent.Equals("Rerun", StringComparison.OrdinalIgnoreCase) ? DrawInputsIntent.Rerun : throw new BadHttpRequestException($"Unknown draw inputs intent '{intent}'. Expected Default or Rerun.");

    // Prefers keys when present; otherwise coerces legacy singular key to a one-element list.
    internal static IReadOnlyList<string>? CoerceDestinationSlotKeys(IReadOnlyList<string>? keys, string? singular) =>
        keys is { Count: > 0 }
            ? keys
            : string.IsNullOrWhiteSpace(singular)
                ? null
                : [singular];
}
