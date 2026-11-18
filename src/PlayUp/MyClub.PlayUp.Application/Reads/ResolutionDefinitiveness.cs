// -----------------------------------------------------------------------
// <copyright file="ResolutionDefinitiveness.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages.ReleaseResolution;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Whether an unconfirmed place can still be confirmed, and whether the release guards pass.
/// The structure read, the grouped preview, and the confirmation all call <see cref="Evaluate"/>.
/// </summary>
public static class ResolutionDefinitiveness
{
    /// <summary>
    /// Judges one occupant.
    /// </summary>
    /// <param name="competition">Owning competition: status, stage ids, and entries.</param>
    /// <param name="source">Source phase when it was loaded; otherwise <see langword="null"/>.</param>
    /// <param name="qualificationPath">
    /// Qualification path that produced the occupant, when the cause depends on ranking scope.
    /// Progression places pass <see langword="null"/>.
    /// </param>
    /// <param name="occupant">Evaluated occupant, including the source-absence detail.</param>
    /// <param name="destination">Phase that stores the occupant. Required to judge <see cref="ResolutionDefinitivenessVerdict.Releasable"/>.</param>
    /// <param name="destinationMatches">
    /// Matches of <paramref name="destination"/>. <see langword="null"/> means they were not loaded,
    /// and the place is not releasable.
    /// </param>
    /// <returns>Definitiveness and the shared release judgment.</returns>
    public static ResolutionDefinitivenessVerdict Evaluate(
        Competition competition,
        Stage? source,
        QualificationPath? qualificationPath,
        ResolutionOccupantAssessment occupant,
        Stage? destination,
        IReadOnlyList<ResolutionReleaseMatch>? destinationMatches)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(occupant);

        var definitive = IsDefinitive(competition, source, qualificationPath, occupant);
        if (destination is null || destinationMatches is null)
        {
            return new ResolutionDefinitivenessVerdict(definitive, false);
        }

        var refusal = ResolutionReleaseRules.Refusal(
            destination,
            new ResolutionReleaseSubject(
                occupant.Target,
                occupant.EntryId,
                occupant.Kind,
                occupant.State,
                occupant.IndeterminacyCause,
                occupant.SlotKey,
                occupant.GroupId),
            destinationMatches);
        return new ResolutionDefinitivenessVerdict(definitive, refusal is null) { RefusalCode = refusal };
    }

    private static bool IsDefinitive(
        Competition competition,
        Stage? source,
        QualificationPath? path,
        ResolutionOccupantAssessment occupant)
    {
        if (occupant.State != StructureResolutionFreshness.Indetermine
            || occupant.IndeterminacyCause is not { } cause)
        {
            return false;
        }

        var sourceCompleted = source is { Status: StageStatus.Completed };
        return cause switch
        {
            StructureResolutionIndeterminacyCause.MatchNonJoue => sourceCompleted,
            StructureResolutionIndeterminacyCause.Egalite => sourceCompleted,
            StructureResolutionIndeterminacyCause.EgaliteClassement =>
                sourceCompleted && OverallAllowsStandingCause(competition, path),
            StructureResolutionIndeterminacyCause.ConditionNonRemplie =>
                sourceCompleted && OverallAllowsStandingCause(competition, path),
            StructureResolutionIndeterminacyCause.SelectionVide =>
                sourceCompleted && !OverallUsesFallbackParticipants(competition, path),
            StructureResolutionIndeterminacyCause.SourceAbsente =>
                SourceAbsenceIsDefinitive(competition, source, occupant),
            _ => false,
        };
    }

    /// <summary>
    /// Overall standings still move when a team can withdraw.
    /// Group and across-groups standings do not.
    /// </summary>
    private static bool OverallAllowsStandingCause(Competition competition, QualificationPath? path) =>
        !IsOverall(path)
        || competition.Status is not (CompetitionStatus.Running or CompetitionStatus.Suspended);

    /// <summary>
    /// With no active entry, an overall standing is rebuilt from match participants.
    /// </summary>
    private static bool OverallUsesFallbackParticipants(Competition competition, QualificationPath? path) =>
        IsOverall(path) && !competition.Entries.Any(entry => entry.Status == EntryStatus.Active);

    private static bool IsOverall(QualificationPath? path)
    {
        if (path is null)
        {
            return false;
        }

        if (path.Source.GroupId is not null || path.Source.Scope == RankingScope.Group)
        {
            return false;
        }

        return path.Source.Scope is not RankingScope.AcrossGroups;
    }

    private static bool SourceAbsenceIsDefinitive(
        Competition competition,
        Stage? source,
        ResolutionOccupantAssessment occupant) =>
        occupant.SourceAbsence switch
        {
            ResolutionSourceAbsence.Unreadable => false,
            ResolutionSourceAbsence.StageNotLoaded =>
                occupant.ParsedSourceStageId is Guid sourceId
                && !competition.StageIds.Any(stageId => stageId.Value == sourceId),
            ResolutionSourceAbsence.PathGone => source is { Status: StageStatus.Completed },
            _ => false,
        };
}

/// <summary>
/// One call of <see cref="ResolutionDefinitiveness.Evaluate"/>.
/// </summary>
/// <param name="Definitive">The place is unconfirmed and can no longer be confirmed.</param>
/// <param name="Releasable">The shared release guards all pass.</param>
public readonly record struct ResolutionDefinitivenessVerdict(bool Definitive, bool Releasable)
{
    /// <summary>
    /// Gets the first release refusal when the destination and its matches were supplied.
    /// </summary>
    public string? RefusalCode { get; init; }
}
