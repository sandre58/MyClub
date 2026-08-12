// -----------------------------------------------------------------------
// <copyright file="DrawConstraintContext.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Auxiliary maps for Required draw constraints (injected by Application/Host — not part of DrawInputs).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DrawConstraintContext"/> class.
/// </remarks>
/// <param name="sourceGroupMap">Optional Entry → source group mapping for <c>SameGroupAvoidance</c>.</param>
/// <param name="teamMap">Optional Entry → team mapping for <c>SameTeamAvoidance</c>.</param>
public sealed class DrawConstraintContext(
    IReadOnlyDictionary<EntryId, GroupId>? sourceGroupMap,
    IReadOnlyDictionary<EntryId, TeamId>? teamMap)
{
    /// <summary>
    /// Gets an empty context (no group / team maps).
    /// </summary>
    public static DrawConstraintContext Empty { get; } = new(null, null);

    /// <summary>
    /// Gets the Entry → source group map when present.
    /// </summary>
    public IReadOnlyDictionary<EntryId, GroupId>? SourceGroupMap { get; } = sourceGroupMap;

    /// <summary>
    /// Gets the Entry → team map when present.
    /// </summary>
    public IReadOnlyDictionary<EntryId, TeamId>? TeamMap { get; } = teamMap;
}
