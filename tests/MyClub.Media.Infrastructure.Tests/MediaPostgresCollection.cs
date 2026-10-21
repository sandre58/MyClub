// -----------------------------------------------------------------------
// <copyright file="MediaPostgresCollection.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace MyClub.Media.Infrastructure.Tests;

[CollectionDefinition(Name)]
[SuppressMessage("Naming", "CA1711", Justification = "xUnit collection naming convention.")]
public sealed class MediaPostgresCollection : ICollectionFixture<MediaPostgresFixture>
{
    public const string Name = "MediaPostgres";
}
