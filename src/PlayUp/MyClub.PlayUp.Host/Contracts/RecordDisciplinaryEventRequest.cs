// -----------------------------------------------------------------------
// <copyright file="RecordDisciplinaryEventRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for recording or correcting a disciplinary fact.
/// </summary>
/// <param name="MemberId">Targeted member identity on the match sheet.</param>
/// <param name="Type">Disciplinary type from the competition catalogue.</param>
public sealed record RecordDisciplinaryEventRequest(Guid MemberId, DisciplinaryType Type);
