// -----------------------------------------------------------------------
// <copyright file="MatchRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using MyClub.Shared.Domain.Enums;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

public class MatchRules(IEnumerable<CardColor> allowedCards) : ValueObject
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private MatchRules()
        : this([CardColor.Red, CardColor.Yellow]) { }

    public static readonly MatchRules Default = new([CardColor.Red, CardColor.Yellow]);

    public IReadOnlyCollection<CardColor> AllowedCards { get; } = allowedCards.ToList().AsReadOnly();

    public override string ToString()
    {
        var str = new StringBuilder(string.Join("|", AllowedCards));

        return str.ToString();
    }
}
