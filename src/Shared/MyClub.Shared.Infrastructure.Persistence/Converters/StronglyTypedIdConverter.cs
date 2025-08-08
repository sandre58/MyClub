// -----------------------------------------------------------------------
// <copyright file="StronglyTypedIdConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Infrastructure.Persistence.Converters;

/// <summary>
/// Entity Framework Core value converter for strongly-typed entity identifiers, enabling safe
/// conversion between domain ID types and their underlying primitive values for database storage.
/// This converter ensures type safety while maintaining efficient database storage.
/// </summary>
/// <typeparam name="TId">The strongly-typed entity identifier type.</typeparam>
/// <remarks>
/// The StronglyTypedIdConverter provides type-safe persistence for strongly-typed identifiers
/// used throughout the domain model, converting between rich domain ID types and primitive
/// GUID values for efficient database storage while preventing ID confusion and type errors.
/// </remarks>
public sealed class StronglyTypedIdConverter<TId>() : ValueConverter<TId, Guid>(static x => x.Value, static x => EntityId.From<TId>(x))
    where TId : EntityId<TId>;

/// <summary>
/// Entity Framework Core value converter for nullable strongly-typed entity identifiers,
/// providing safe conversion with proper null handling between optional domain ID types
/// and their underlying primitive values for database storage.
/// </summary>
/// <typeparam name="TId">The strongly-typed entity identifier type.</typeparam>
/// <remarks>
/// The NullableStronglyTypedIdConverter extends the strongly-typed ID conversion pattern
/// to support optional relationships and nullable foreign keys while maintaining type safety
/// and proper null value handling in database operations.
/// </remarks>
public sealed class NullableStronglyTypedIdConverter<TId>() : ValueConverter<TId?, Guid?>(static x => x != null ? x.Value : null, static x => x.HasValue ? EntityId.From<TId>(x.Value) : null)
    where TId : EntityId<TId>;
