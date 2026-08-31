// -----------------------------------------------------------------------
// <copyright file="RecordSubstitutionRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for recording or correcting a substitution fact.
/// </summary>
/// <param name="OutMemberId">Member leaving the field.</param>
/// <param name="InMemberId">Member entering the field.</param>
/// <param name="Side">Match side for this substitution.</param>
public sealed record RecordSubstitutionRequest(
    Guid OutMemberId,
    Guid InMemberId,
    Side Side);
