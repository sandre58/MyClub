// -----------------------------------------------------------------------
// <copyright file="HostPostgresCollectionDefinition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[CollectionDefinition("host-postgres")]
public sealed class HostPostgresCollectionDefinition : ICollectionFixture<HostPostgresFixture>;
