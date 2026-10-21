// -----------------------------------------------------------------------
// <copyright file="RenameEntryRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for renaming a competition entry.
/// </summary>
/// <param name="DisplayName">New display name.</param>
public sealed record RenameEntryRequest(string DisplayName);
