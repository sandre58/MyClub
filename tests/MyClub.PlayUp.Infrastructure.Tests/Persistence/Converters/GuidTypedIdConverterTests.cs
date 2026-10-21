// -----------------------------------------------------------------------
// <copyright file="GuidTypedIdConverterTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence.Converters;

public sealed class GuidTypedIdConverterTests
{
    private readonly GuidTypedIdConverter<CompetitionId> _converter = new();

    [Fact]
    public void Convert_round_trips_CompetitionId()
    {
        var id = CompetitionId.New();

        var provider = _converter.ConvertToProvider(id);
        var restored = _converter.ConvertFromProvider(provider);

        provider.Should().Be(id.Value);
        restored.Should().Be(id);
    }

    [Fact]
    public void ConvertFromProvider_rejects_empty_guid_with_domain_exception()
    {
        var act = () => _converter.ConvertFromProvider(Guid.Empty);

        act.Should().Throw<DomainException>()
            .WithMessage("*CompetitionId*empty*");
    }
}
