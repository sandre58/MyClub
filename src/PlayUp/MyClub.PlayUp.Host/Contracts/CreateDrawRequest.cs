// -----------------------------------------------------------------------
// <copyright file="CreateDrawRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for creating a Draw.
/// </summary>
/// <param name="Kind">Slot | Group | Pairing (case-insensitive).</param>
public sealed record CreateDrawRequest(string Kind);
