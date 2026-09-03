// -----------------------------------------------------------------------
// <copyright file="DeclaredMemberDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Declared roster member on a competition entry.
/// </summary>
/// <param name="MemberId">Member identity within the entry roster.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="Role">Player or staff for this participation.</param>
/// <param name="ReferencedOnMatchSheet">
/// True when the member is still listed on any match composition sheet for this entry
/// (same gate as <c>RemoveDeclaredMember</c> — UI may disable remove before the click).
/// </param>
public sealed record DeclaredMemberDto(
    Guid MemberId,
    string DisplayName,
    DeclaredMemberRole Role,
    bool ReferencedOnMatchSheet = false);
