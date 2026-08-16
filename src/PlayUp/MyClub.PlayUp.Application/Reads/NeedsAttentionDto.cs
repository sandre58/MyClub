// -----------------------------------------------------------------------
// <copyright file="NeedsAttentionDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Needs Attention list for hub À traiter (derived Read — not persisted).
/// </summary>
/// <param name="CompetitionId">Competition identity.</param>
/// <param name="Items">Attention items.</param>
public sealed record NeedsAttentionDto(
    Guid CompetitionId,
    IReadOnlyList<NeedsAttentionItemDto> Items)
{
    /// <summary>Gets the number of attention items.</summary>
    public int Count => Items.Count;
}
