// -----------------------------------------------------------------------
// <copyright file="QualificationSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Origin of participants for a qualification path (ranking scope and optional group).
/// No navigable reference to a Stage aggregate.
/// </summary>
public sealed record QualificationSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationSource"/> class.
    /// </summary>
    /// <param name="scope">
    /// Ranking scope; when <see langword="null"/> and <paramref name="groupId"/> is null, overall stage ranking is implied.
    /// </param>
    /// <param name="groupId">Group identity when selecting from a specific group; otherwise <see langword="null"/>.</param>
    public QualificationSource(RankingScope? scope = null, GroupId? groupId = null)
    {
        scope = scope switch
        {
            { } definedScope when !Enum.IsDefined(definedScope) => throw new DomainException(
                "Ranking scope is unknown.", RulesErrorCodes.QualificationRulesInvalid),
            RankingScope.Group when groupId is null => throw new DomainException(
                "Group ranking scope requires a group identity.", RulesErrorCodes.QualificationRulesInvalid),
            RankingScope.Overall when groupId is not null => throw new DomainException(
                "Overall ranking scope cannot target a specific group.", RulesErrorCodes.QualificationRulesInvalid),
            null when groupId is not null => RankingScope.Group,
            _ => scope
        };

        Scope = scope;
        GroupId = groupId;
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
}
