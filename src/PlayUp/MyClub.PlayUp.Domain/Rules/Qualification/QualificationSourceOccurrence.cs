// -----------------------------------------------------------------------
// <copyright file="QualificationSourceOccurrence.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Stable identity of one expanded selection occurrence (override key — not a list index).
/// </summary>
public sealed record QualificationSourceOccurrence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationSourceOccurrence"/> class.
    /// </summary>
    public QualificationSourceOccurrence(
        RankingScope scope,
        int position,
        GroupId? groupId = null,
        int? acrossGroupsPosition = null)
    {
        if (!Enum.IsDefined(scope))
        {
            throw new DomainException(
                "Ranking scope is unknown.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (position < 1)
        {
            throw new DomainException(
                "Occurrence position must be at least 1.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        switch (scope)
        {
            case RankingScope.Group when groupId is null:
                throw new DomainException(
                    "Group occurrence requires a group identity.",
                    RulesErrorCodes.QualificationRulesInvalid);
            case RankingScope.Overall when groupId is not null || acrossGroupsPosition is not null:
                throw new DomainException(
                    "Overall occurrence cannot carry group or across-groups position.",
                    RulesErrorCodes.QualificationRulesInvalid);
            case RankingScope.AcrossGroups when acrossGroupsPosition is null or < 1:
                throw new DomainException(
                    "Across-groups occurrence requires a position P ≥ 1.",
                    RulesErrorCodes.QualificationRulesInvalid);
            case RankingScope.AcrossGroups when groupId is not null:
                throw new DomainException(
                    "Across-groups occurrence cannot target a specific group.",
                    RulesErrorCodes.QualificationRulesInvalid);
            case RankingScope.Group when acrossGroupsPosition is not null:
                throw new DomainException(
                    "Group occurrence cannot carry an across-groups position.",
                    RulesErrorCodes.QualificationRulesInvalid);
            case RankingScope.Group:
            case RankingScope.Overall:
            case RankingScope.AcrossGroups:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
        }

        Scope = scope;
        Position = position;
        GroupId = groupId;
        AcrossGroupsPosition = acrossGroupsPosition;
    }

    /// <summary>Gets the ranking scope of this occurrence.</summary>
    public RankingScope Scope { get; }

    /// <summary>Gets the 1-based selection position k.</summary>
    public int Position { get; }

    /// <summary>Gets the group when <see cref="Scope"/> is <see cref="RankingScope.Group"/>.</summary>
    public GroupId? GroupId { get; }

    /// <summary>Gets P when <see cref="Scope"/> is <see cref="RankingScope.AcrossGroups"/>.</summary>
    public int? AcrossGroupsPosition { get; }

    /// <summary>Creates a group occurrence.</summary>
    public static QualificationSourceOccurrence Group(GroupId groupId, int position) =>
        new(RankingScope.Group, position, groupId);

    /// <summary>Creates an overall occurrence.</summary>
    public static QualificationSourceOccurrence Overall(int position) =>
        new(RankingScope.Overall, position);

    /// <summary>Creates an across-groups occurrence.</summary>
    public static QualificationSourceOccurrence AcrossGroups(int acrossGroupsPosition, int position) =>
        new(RankingScope.AcrossGroups, position, acrossGroupsPosition: acrossGroupsPosition);
}
