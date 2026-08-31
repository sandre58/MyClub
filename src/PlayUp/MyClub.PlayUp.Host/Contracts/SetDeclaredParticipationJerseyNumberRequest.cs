// -----------------------------------------------------------------------
// <copyright file="SetDeclaredParticipationJerseyNumberRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for setting or clearing a jersey number on the match sheet.
/// </summary>
/// <param name="JerseyNumber">Jersey number, or <see langword="null"/> to clear.</param>
public sealed record SetDeclaredParticipationJerseyNumberRequest(int? JerseyNumber);
