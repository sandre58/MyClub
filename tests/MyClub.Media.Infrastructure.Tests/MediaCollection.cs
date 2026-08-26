// -----------------------------------------------------------------------
// <copyright file="MediaCollection.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Xunit;

namespace MyClub.Media.Infrastructure.Tests;

[CollectionDefinition(Name)]
public sealed class MediaPostgresCollection : ICollectionFixture<MediaPostgresFixture>
{
    public const string Name = "MediaPostgres";
}
