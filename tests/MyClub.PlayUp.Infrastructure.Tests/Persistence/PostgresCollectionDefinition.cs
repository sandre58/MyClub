// -----------------------------------------------------------------------
// <copyright file="PostgresCollectionDefinition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[CollectionDefinition("postgres")]
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit collection definitions must be public.")]
public sealed class PostgresCollectionDefinition : ICollectionFixture<PostgresFixture>;
