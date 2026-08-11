// -----------------------------------------------------------------------
// <copyright file="QualificationSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Origin of participants for a qualification path (ranking scope and optional group or across-groups position).
/// No navigable reference to a Stage aggregate.
/// </summary>
/// <remarks>
/// <see cref="RankingScope.AcrossGroups"/> builds a candidate universe from
/// <see cref="AcrossGroupsPosition"/> of each group standing (Application assembles the derived standing).
/// It is not a synonym of Overall + Best.
/// </remarks>
public sealed record QualificationSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationSource"/> class.
    /// </summary>
    /// <param name="scope">
    /// Ranking scope; when <see langword="null"/> and <paramref name="groupId"/> is null, overall stage ranking is implied.
    /// </param>
    /// <param name="groupId">Group identity when selecting from a specific group; otherwise <see langword="null"/>.</param>
    /// <param name="acrossGroupsPosition">
    /// 1-based position extracted from each group when <paramref name="scope"/> is
    /// <see cref="RankingScope.AcrossGroups"/>; otherwise <see langword="null"/>.
    /// </param>
    public QualificationSource(
        RankingScope? scope = null,
        GroupId? groupId = null,
        int? acrossGroupsPosition = null)
    {
        scope = scope switch
        {
            { } definedScope when !Enum.IsDefined(definedScope) => throw new DomainException(
                "Ranking scope is unknown.", RulesErrorCodes.QualificationRulesInvalid),
            RankingScope.Group when groupId is null => throw new DomainException(
                "Group ranking scope requires a group identity.", RulesErrorCodes.QualificationRulesInvalid),
            RankingScope.Overall when groupId is not null => throw new DomainException(
                "Overall ranking scope cannot target a specific group.", RulesErrorCodes.QualificationRulesInvalid),
            RankingScope.Overall when acrossGroupsPosition is not null => throw new DomainException(
                "Overall ranking scope cannot carry an across-groups position.",
                RulesErrorCodes.QualificationRulesInvalid),
            RankingScope.AcrossGroups when groupId is not null => throw new DomainException(
                "Across-groups ranking scope cannot target a specific group.",
                RulesErrorCodes.QualificationRulesInvalid),
            RankingScope.AcrossGroups when acrossGroupsPosition is null => throw new DomainException(
                "Across-groups ranking scope requires a position.",
                RulesErrorCodes.QualificationRulesInvalid),
            RankingScope.AcrossGroups when acrossGroupsPosition < 1 => throw new DomainException(
                "Across-groups position must be at least 1.",
                RulesErrorCodes.QualificationRulesInvalid),
            RankingScope.Group when acrossGroupsPosition is not null => throw new DomainException(
                "Group ranking scope cannot carry an across-groups position.",
                RulesErrorCodes.QualificationRulesInvalid),
            null when acrossGroupsPosition is not null => throw new DomainException(
                "Across-groups position requires AcrossGroups ranking scope.",
                RulesErrorCodes.QualificationRulesInvalid),
            null when groupId is not null => RankingScope.Group,
            _ => scope
        };

        Scope = scope;
        GroupId = groupId;
        AcrossGroupsPosition = acrossGroupsPosition;
    }

    /// <summary>
    /// Gets the ranking scope when explicit; otherwise <see langword="null"/> (overall stage ranking implied).
    /// </summary>
    public RankingScope? Scope { get; }

    /// <summary>
    /// Gets the group identity when selecting from a group; otherwise <see langword="null"/>.
    /// </summary>
    public GroupId? GroupId { get; }

    /// <summary>
    /// Gets the 1-based group position used to build an across-groups candidate universe;
    /// otherwise <see langword="null"/>.
    /// </summary>
    public int? AcrossGroupsPosition { get; }

    /// <summary>
    /// Creates a source for a specific group ranking.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <returns>A group-scoped qualification source.</returns>
    public static QualificationSource FromGroup(GroupId groupId) =>
        new(RankingScope.Group, groupId);

    /// <summary>
    /// Creates a source for the overall stage ranking.
    /// </summary>
    /// <returns>An overall qualification source.</returns>
    public static QualificationSource Overall() => new(RankingScope.Overall);

    /// <summary>
    /// Creates a source that selects the given position from each group, then ranks those candidates together.
    /// </summary>
    /// <param name="position">1-based standing position extracted from each group (≥ 1).</param>
    /// <returns>An across-groups qualification source.</returns>
    public static QualificationSource AcrossGroups(int position) =>
        new(RankingScope.AcrossGroups, groupId: null, acrossGroupsPosition: position);
}
