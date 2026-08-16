// -----------------------------------------------------------------------
// <copyright file="ApplicationErrorCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application;

/// <summary>
/// Stable machine-readable codes for application orchestration failures.
/// </summary>
public static class ApplicationErrorCodes
{
    /// <summary>
    /// Gets the code when an inbound feed targets a missing slot on the destination stage.
    /// </summary>
    public const string DanglingFeedTarget = "Application.DanglingFeedTarget";

    /// <summary>
    /// Gets the code when the target stage is not in the provided competition stages list.
    /// </summary>
    public const string StageNotInCompetition = "Application.StageNotInCompetition";

    /// <summary>
    /// Gets the code when a stage identity cannot be loaded from persistence.
    /// </summary>
    public const string StageNotFound = "Application.StageNotFound";

    /// <summary>
    /// Gets the code when a competition identity cannot be loaded from persistence.
    /// </summary>
    public const string CompetitionNotFound = "Application.CompetitionNotFound";

    /// <summary>
    /// Gets the code when a match identity cannot be loaded from persistence.
    /// </summary>
    public const string MatchNotFound = "Application.MatchNotFound";

    /// <summary>
    /// Gets the code when WhoFeeds validation fails before Prepare.
    /// </summary>
    public const string SlotFeedsInvalid = "Application.SlotFeedsInvalid";

    /// <summary>
    /// Gets the code when Fixture↔Match coherence fails for progression (attachments / matches / legs).
    /// </summary>
    public const string FixtureInvalid = "Application.FixtureInvalid";

    /// <summary>
    /// Gets the code when a Round.TieFormat is required but missing:
    /// PrepareStage (Progression references a Round fixture), or ApplyProgressionOutcome
    /// (missing format, or fixture not hosted by a Round).
    /// </summary>
    public const string TieFormatRequired = "Application.TieFormatRequired";

    /// <summary>
    /// Legacy code: group-scoped paths once rejected before multi-Standing orchestration (7.0.9.2).
    /// Prefer <see cref="QualificationStandingMissing"/> / <see cref="QualificationGroupNotFound"/>.
    /// </summary>
    public const string QualificationSourceNotSupported = "Application.QualificationSourceNotSupported";

    /// <summary>
    /// Gets the code when a qualification path needs a standing that was not supplied.
    /// </summary>
    public const string QualificationStandingMissing = "Application.QualificationStandingMissing";

    /// <summary>
    /// Gets the code when a qualification path references a group absent from the source stage.
    /// </summary>
    public const string QualificationGroupNotFound = "Application.QualificationGroupNotFound";

    /// <summary>
    /// Gets the code when AcrossGroups extraction yields no candidates.
    /// </summary>
    public const string QualificationCandidatesEmpty = "Application.QualificationCandidatesEmpty";

    /// <summary>
    /// Gets the code when AcrossGroups extraction finds the same entry more than once.
    /// </summary>
    public const string QualificationCandidateDuplicate = "Application.QualificationCandidateDuplicate";

    /// <summary>
    /// Gets the code when AcrossGroups paths are applied without a matches list.
    /// </summary>
    public const string QualificationMatchesRequired = "Application.QualificationMatchesRequired";

    /// <summary>
    /// Gets the code when a published Draw resolution cannot be applied to the current Stage state.
    /// Distinct from Domain <c>NoSolution</c> (solver found no resolution).
    /// </summary>
    public const string DrawApplyFailure = "Application.DrawApplyFailure";

    /// <summary>
    /// Gets the code when ApplyDraw does not support the Draw resolution kind (e.g. Pairing in V1.A).
    /// </summary>
    public const string DrawKindNotSupported = "Application.DrawKindNotSupported";

    /// <summary>
    /// Gets the code when GenerateDrawResolution cannot run (e.g. not Draft, missing inputs).
    /// Distinct from Domain <c>DrawGenerationInvalid</c> (malformed request) and <c>NoSolution</c>.
    /// </summary>
    public const string DrawGenerationFailure = "Application.DrawGenerationFailure";

    /// <summary>
    /// Gets the code when GenerateSchedule cannot run (e.g. target match not attached to the Stage).
    /// Distinct from Domain <c>SchedulingResult.InvalidRequest</c> / <c>NoSolution</c>.
    /// </summary>
    public const string ScheduleGenerationFailure = "Application.ScheduleGenerationFailure";

    /// <summary>
    /// Gets the code when ApplySchedule cannot run because the result is not Success
    /// (NoSolution or InvalidRequest). Distinct from Domain placement invariants.
    /// </summary>
    public const string ScheduleApplyFailure = "Application.ScheduleApplyFailure";

    /// <summary>
    /// Gets the code when AddEntry would exceed <c>EntryRules.MaximumTeams</c>.
    /// </summary>
    public const string EntryCapacityExceeded = "Application.EntryCapacityExceeded";

    /// <summary>
    /// Gets the code when a ConfigureStructure intent has invalid parameters.
    /// </summary>
    public const string InvalidStructureIntent = "Application.InvalidStructureIntent";

    /// <summary>
    /// Gets the code when Cup bracket size is not a supported power of two (V1 bound).
    /// </summary>
    public const string CupBracketNotPowerOfTwo = "Application.CupBracketNotPowerOfTwo";

    /// <summary>
    /// Gets the code when Organisation mutations are refused for the current lifecycle status.
    /// </summary>
    public const string OrganisationNotMutable = "Application.OrganisationNotMutable";

    /// <summary>
    /// Gets the code when Fixture/Match materialization cannot run.
    /// </summary>
    public const string MaterializationFailure = "Application.MaterializationFailure";

    /// <summary>
    /// Gets the code when Start/Finish is refused because the Competition is Completed or Archived.
    /// </summary>
    public const string MatchOperationNotAllowed = "Application.MatchOperationNotAllowed";

    /// <summary>
    /// Gets the code when Qualification/Progression is refused because the Competition is Completed or Archived.
    /// </summary>
    public const string ConsequenceOperationNotAllowed = "Application.ConsequenceOperationNotAllowed";

    /// <summary>
    /// Gets the code when ApplyProgression would overwrite a dynamic slot occupant (F8 preflight).
    /// </summary>
    public const string SlotOccupancyConflict = "Application.SlotOccupancyConflict";
}
