// -----------------------------------------------------------------------
// <copyright file="CompleteCompetitionRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// Request body for <c>POST /competitions/{id}/complete</c>.
/// </summary>
/// <param name="Mode">Completion manner: Normal, Administrative, or Abandoned.</param>
public sealed record CompleteCompetitionRequest(string Mode);
