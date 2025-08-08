// -----------------------------------------------------------------------
// <copyright file="StandingColumnListConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Infrastructure.Persistence.Converters;

namespace MyClub.Scorer.Infrastructure.Persistence.Converters;

/// <summary>
/// Entity Framework Core value converter for lists of standing columns, enabling storage of
/// column configurations as delimited strings in the database. This converter specializes
/// the ListConverter for standing column management with proper type conversion.
/// </summary>
/// <param name="delimiter">Character used to separate column keys in the database string.</param>
/// <remarks>
/// The StandingColumnListConverter provides efficient storage for standing table column
/// configurations, converting between domain column objects and their string representations
/// while maintaining proper type information and column ordering for league table displays.
/// </remarks>
public class StandingColumnListConverter(char delimiter = ';') : ListConverter<IStandingColumn>(static x => x.Key, static x => StandingColumn.Create(Enum.Parse<StandingColumnType>(x)), delimiter);
