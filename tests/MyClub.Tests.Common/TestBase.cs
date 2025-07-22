// -----------------------------------------------------------------------
// <copyright file="TestBase.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;

namespace MyClub.Tests.Common;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Base class for tests")]
public abstract class TestBase
{
    protected IFixture Fixture { get; }

    protected TestBase()
    {
        Fixture = new Fixture();
        Fixture.Customize(new AutoMoqCustomization { ConfigureMembers = true });
    }

    protected Mock<T> FreezeMock<T>()
        where T : class
        => Fixture.Freeze<Mock<T>>();

    /// <summary>
    /// Crée un objet avec données générées.
    /// </summary>
    protected T Create<T>() => Fixture.Create<T>();

    /// <summary>
    /// Crée une collection d’objets.
    /// </summary>
    protected IEnumerable<T> CreateMany<T>(int count = 3) => Fixture.CreateMany<T>(count);
}
