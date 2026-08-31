// -----------------------------------------------------------------------
// <copyright file="ChangeDeclaredMemberRoleRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for changing a declared member role.
/// </summary>
/// <param name="Role">New player/staff role.</param>
public sealed record ChangeDeclaredMemberRoleRequest(DeclaredMemberRole Role);
