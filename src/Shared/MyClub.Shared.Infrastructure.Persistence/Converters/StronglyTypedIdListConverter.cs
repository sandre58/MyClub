// -----------------------------------------------------------------------
// <copyright file="StronglyTypedIdListConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Infrastructure.Persistence.Converters;

/// <summary>
/// Entity Framework Core value converter for collections of strongly-typed entity identifiers,
/// enabling storage of ID lists as delimited strings in the database. This converter extends
/// the ListConverter pattern specifically for strongly-typed ID collections.
/// </summary>
/// <typeparam name="TId">The strongly-typed entity identifier type.</typeparam>
/// <param name="delimiter">Character used to separate ID values in the database string.</param>
/// <remarks>
/// The StronglyTypedIdListConverter provides efficient storage for collections of domain
/// identifiers while maintaining type safety and proper conversion between strongly-typed
/// IDs and their underlying primitive values for database persistence.
/// </remarks>
public class StronglyTypedIdListConverter<TId>(char delimiter = ';') : ListConverter<TId>(static x => x.Value.ToString(), EntityId.From<TId>, delimiter)
    where TId : EntityId<TId>;
