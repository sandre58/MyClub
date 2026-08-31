// -----------------------------------------------------------------------
// <copyright file="RenameDeclaredMemberRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for renaming a declared member.
/// </summary>
/// <param name="DisplayName">New display name.</param>
public sealed record RenameDeclaredMemberRequest(string DisplayName);
