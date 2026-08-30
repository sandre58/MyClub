// -----------------------------------------------------------------------
// <copyright file="MatchErrorCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// Stable machine-readable codes for Match aggregate invariant violations.
/// </summary>
public static class MatchErrorCodes
{
    /// <summary>
    /// Gets the code for an invalid lifecycle transition.
    /// </summary>
    public const string InvalidTransition = "Match.InvalidTransition";

    /// <summary>
    /// Gets the code when home and away entry identities are the same.
    /// </summary>
    public const string SameParticipant = "Match.SameParticipant";

    /// <summary>
    /// Gets the code when a match result or score is invalid.
    /// </summary>
    public const string InvalidResult = "Match.InvalidResult";

    /// <summary>
    /// Gets the code when a result is already recorded (Finished).
    /// </summary>
    public const string ResultAlreadyRecorded = "Match.ResultAlreadyRecorded";

    /// <summary>
    /// Gets the code when the match composition cannot be mutated in the current status.
    /// </summary>
    public const string CompositionImmutable = "Match.CompositionImmutable";

    /// <summary>
    /// Gets the code when a MemberId is already present on the match composition.
    /// </summary>
    public const string DuplicateParticipation = "Match.DuplicateParticipation";

    /// <summary>
    /// Gets the code when a declared participation is not found on the match.
    /// </summary>
    public const string ParticipationNotFound = "Match.ParticipationNotFound";

    /// <summary>
    /// Gets the code when a jersey number is already used on the same side.
    /// </summary>
    public const string DuplicateJerseyNumber = "Match.DuplicateJerseyNumber";

    /// <summary>
    /// Gets the code when a composition status value is invalid.
    /// </summary>
    public const string InvalidCompositionStatus = "Match.InvalidCompositionStatus";

    /// <summary>
    /// Gets the code when a match side value is invalid.
    /// </summary>
    public const string InvalidSide = "Match.InvalidSide";

    /// <summary>
    /// Gets the code when a running score value is invalid.
    /// </summary>
    public const string InvalidRunningScore = "Match.InvalidRunningScore";

    /// <summary>
    /// Gets the code when the running score cannot be mutated in the current status.
    /// </summary>
    public const string RunningScoreImmutable = "Match.RunningScoreImmutable";

    /// <summary>
    /// Gets the code when a nominative goal mutation is not allowed in the current status
    /// (or when create/remove is attempted after Finish with an observed Live).
    /// </summary>
    public const string RecordedGoalMutationNotAllowed = "Match.RecordedGoalMutationNotAllowed";

    /// <summary>
    /// Gets the code when a recorded goal is not found on the match.
    /// </summary>
    public const string RecordedGoalNotFound = "Match.RecordedGoalNotFound";

    /// <summary>
    /// Gets the code when an assister is supplied for an own goal (CSC).
    /// </summary>
    public const string AssisterNotAllowedOnOwnGoal = "Match.AssisterNotAllowedOnOwnGoal";

    /// <summary>
    /// Gets the code when the assister member identity equals the scorer.
    /// </summary>
    public const string AssisterSameAsScorer = "Match.AssisterSameAsScorer";

    /// <summary>
    /// Gets the code when removing a declared participation that is still referenced by a recorded goal.
    /// </summary>
    public const string ParticipationReferencedByRecordedGoal = "Match.ParticipationReferencedByRecordedGoal";

    /// <summary>
    /// Gets the code when a substitution mutation is not allowed in the current status
    /// (or when create/remove is attempted after Finish with an observed Live).
    /// </summary>
    public const string SubstitutionMutationNotAllowed = "Match.SubstitutionMutationNotAllowed";

    /// <summary>
    /// Gets the code when a recorded substitution is not found on the match.
    /// </summary>
    public const string SubstitutionNotFound = "Match.SubstitutionNotFound";

    /// <summary>
    /// Gets the code when the outgoing and incoming members are the same.
    /// </summary>
    public const string SubstitutionSameMember = "Match.SubstitutionSameMember";

    /// <summary>
    /// Gets the code when out/in members are not both on the substitution side.
    /// </summary>
    public const string SubstitutionSideMismatch = "Match.SubstitutionSideMismatch";

    /// <summary>
    /// Gets the code when a substitution is inconsistent with derived on-field presence.
    /// </summary>
    public const string SubstitutionPresenceInvalid = "Match.SubstitutionPresenceInvalid";

    /// <summary>
    /// Gets the code when removing a declared participation that is still referenced by a recorded substitution.
    /// </summary>
    public const string ParticipationReferencedBySubstitution = "Match.ParticipationReferencedBySubstitution";
}
