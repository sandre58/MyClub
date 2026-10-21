// -----------------------------------------------------------------------
// <copyright file="AddDeclaredMemberRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for adding a declared member to an entry roster.
/// </summary>
/// <param name="DisplayName">Member display name.</param>
/// <param name="Role">Player or staff for this participation.</param>
public sealed record AddDeclaredMemberRequest(string DisplayName, DeclaredMemberRole Role);
