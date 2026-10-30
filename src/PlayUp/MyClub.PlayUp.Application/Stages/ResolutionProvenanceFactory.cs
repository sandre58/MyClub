// -----------------------------------------------------------------------
// <copyright file="ResolutionProvenanceFactory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Builds persisted resolution provenance witnesses for Apply use cases.
/// </summary>
public static class ResolutionProvenanceFactory
{
    /// <summary>
    /// Creates qualification Apply provenance from a path and standing view.
    /// </summary>
    public static ResolutionProvenance ForQualificationApply(
        StageId sourceStageId,
        QualificationPath path,
        Standing standing,
        EntryId entryId) =>
        new(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(sourceStageId, path),
            ResolutionUpstreamFingerprint.ForQualificationSelection(standing, entryId));

    /// <summary>
    /// Creates progression Apply provenance from a path and fixture outcome.
    /// </summary>
    public static ResolutionProvenance ForProgressionApply(
        StageId sourceStageId,
        ProgressionPath path,
        FixtureOutcome outcome) =>
        new(
            ResolutionProvenanceKind.Progression,
            ResolutionPathIdentifier.ForProgression(sourceStageId, path),
            ResolutionUpstreamFingerprint.ForProgressionOutcome(outcome, path.Outcome));
}
