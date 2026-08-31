// -----------------------------------------------------------------------
// <copyright file="ChangeDeclaredParticipationCompositionStatusRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for changing starter/bench status on the match sheet.
/// </summary>
/// <param name="CompositionStatus">New composition status.</param>
public sealed record ChangeDeclaredParticipationCompositionStatusRequest(CompositionStatus CompositionStatus);
