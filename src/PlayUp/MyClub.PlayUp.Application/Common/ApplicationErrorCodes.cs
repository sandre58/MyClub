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
    /// Gets the code when ProgressionRules exist but no Path matches the fixture's structural PairKey.
    /// Distinct from empty rules (no-op Apply).
    /// </summary>
    public const string ProgressionPathNotFound = "Application.ProgressionPathNotFound";

    /// <summary>
    /// Gets the code when ApplyProgressionOutcome cannot resolve a Round for the fixture
    /// (e.g. Matchday fixture — Progression requires a Round-hosted Fixture).
    /// Null <c>Round.TieFormat</c> is not an error (effective OneLeg).
    /// </summary>
    public const string TieFormatRequired = "Application.TieFormatRequired";

    /// <summary>
    /// Historical code: group-scoped paths once rejected before multi-Standing orchestration (7.0.9.2).
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
    /// Gets the code when ApplyDraw does not support the Draw resolution kind.
    /// </summary>
    public const string DrawKindNotSupported = "Application.DrawKindNotSupported";

    /// <summary>
    /// Gets the code when GenerateDrawResolution cannot run (e.g. not Draft, missing inputs).
    /// Distinct from Domain <c>DrawGenerationInvalid</c> (malformed request) and <c>NoSolution</c>.
    /// </summary>
    public const string DrawGenerationFailure = "Application.DrawGenerationFailure";

    /// <summary>
    /// Gets the code when CreateDraw is refused because DrawRules are not engaged on the stage.
    /// </summary>
    public const string DrawRulesRequired = "Application.DrawRulesRequired";

    /// <summary>
    /// Gets the code when ReleaseDrawAlignedPlacements cannot run (e.g. not Resolved Slot).
    /// Distinct from Domain <c>SlotFeedConflict</c> (DirectAssignment on a target slot).
    /// </summary>
    public const string DrawReleaseFailure = "Application.DrawReleaseFailure";

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
    /// Gets the code when Cup bracket size is not a supported power of two.
    /// </summary>
    public const string CupBracketNotPowerOfTwo = "Application.CupBracketNotPowerOfTwo";

    /// <summary>
    /// Gets the code when Structure mutations are refused for the current lifecycle status.
    /// </summary>
    public const string StructureNotMutable = "Application.StructureNotMutable";

    /// <summary>
    /// Gets the code when a stage skeleton rebuild attempts to change <c>StructureFormatKind</c>.
    /// </summary>
    public const string StructureFormatImmutable = "Application.StructureFormatImmutable";

    /// <summary>
    /// Gets the code when Fixture/Match materialization cannot run.
    /// </summary>
    public const string MaterializationFailure = "Application.MaterializationFailure";

    /// <summary>
    /// Gets the code when Swiss <c>GenerateNextRound</c> fails (preconditions or I8 NoSolution).
    /// </summary>
    public const string SwissRoundGenerationFailure = "Application.SwissRoundGenerationFailure";

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

    /// <summary>
    /// Gets the code when Complete(Normal) is refused because the competition is not sportively complete.
    /// </summary>
    public const string CompletionNotAllowed = "Application.CompletionNotAllowed";

    /// <summary>
    /// Gets the code when the completion mode string cannot be parsed.
    /// </summary>
    public const string InvalidCompletionMode = "Application.InvalidCompletionMode";

    /// <summary>
    /// Gets the code when a normal lifecycle mutation is refused because the Competition is Completed or Archived.
    /// </summary>
    public const string CompetitionClosed = "Application.CompetitionClosed";

    /// <summary>
    /// Gets the code when a logo Media reference does not exist.
    /// </summary>
    public const string MediaNotFound = "Application.MediaNotFound";

    /// <summary>
    /// Gets the code when an inbound declared member role is not a defined enum value.
    /// </summary>
    public const string InvalidDeclaredMemberRole = "Application.InvalidDeclaredMemberRole";

    /// <summary>
    /// Gets the code when removing a declared member is refused because a match sheet still references them.
    /// </summary>
    public const string DeclaredMemberReferencedByMatchSheet = "Application.DeclaredMemberReferencedByMatchSheet";

    /// <summary>
    /// Gets the code when a declared member identity is not on the entry roster.
    /// </summary>
    public const string DeclaredMemberNotFound = "Application.DeclaredMemberNotFound";

    /// <summary>
    /// Gets the code when a named entry lot is empty.
    /// </summary>
    public const string EmptyEntryLot = "Application.EmptyEntryLot";

    /// <summary>
    /// Gets the code when a named entry lot contains duplicate identities.
    /// </summary>
    public const string DuplicateEntryLotId = "Application.DuplicateEntryLotId";

    /// <summary>
    /// Gets the code when a lot withdraw is refused because an entry is not Active.
    /// </summary>
    public const string EntryNotWithdrawable = "Application.EntryNotWithdrawable";

    /// <summary>
    /// Gets the code when an inbound match side is not a defined enum value.
    /// </summary>
    public const string InvalidSide = "Application.InvalidSide";

    /// <summary>
    /// Gets the code when an inbound composition status is not a defined enum value.
    /// </summary>
    public const string InvalidCompositionStatus = "Application.InvalidCompositionStatus";

    /// <summary>
    /// Gets the code when a member cannot be added to a match sheet (roster, role, entry status).
    /// </summary>
    public const string ParticipationNotEligible = "Application.ParticipationNotEligible";

    /// <summary>
    /// Gets the code when a match does not belong to the supplied competition.
    /// </summary>
    public const string MatchNotInCompetition = "Application.MatchNotInCompetition";

    /// <summary>
    /// Gets the code when an inbound disciplinary type is not a defined enum value.
    /// </summary>
    public const string InvalidDisciplinaryType = "Application.InvalidDisciplinaryType";

    /// <summary>
    /// Gets the code when a disciplinary type is not authorized by competition rules.
    /// </summary>
    public const string DisciplinaryTypeNotAllowed = "Application.DisciplinaryTypeNotAllowed";

    /// <summary>
    /// Gets the code when RemoveCompetitionStage is refused because the competition has a single stage.
    /// </summary>
    public const string LastStageCannotBeRemoved = "Application.LastStageCannotBeRemoved";
}
