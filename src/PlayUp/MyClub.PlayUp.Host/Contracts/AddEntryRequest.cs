// -----------------------------------------------------------------------
// <copyright file="AddEntryRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for adding a competition entry.
/// </summary>
/// <param name="DisplayName">Entry display name.</param>
/// <param name="TeamId">Optional team identity; generated when omitted.</param>
public sealed record AddEntryRequest(string DisplayName, Guid? TeamId = null);
