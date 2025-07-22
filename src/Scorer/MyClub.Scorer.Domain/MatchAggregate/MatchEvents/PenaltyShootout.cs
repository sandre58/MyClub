// -----------------------------------------------------------------------
// <copyright file="PenaltyShootout.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.MatchAggregate.MatchEvents;

public class PenaltyShootout : Entity<PenaltyShootoutId>
{
    // <remarks>Used by EF Core</remarks>
    private PenaltyShootout()
        : base() { }

    private PenaltyShootout(PenaltyShootoutId id, PlayerId? takerId = null, PenaltyShootoutOutcome result = PenaltyShootoutOutcome.None)
        : base(id)
    {
        TakerId = takerId;
        Result = result;
    }

    public static PenaltyShootout Create(PlayerId? takerId = null, PenaltyShootoutOutcome result = PenaltyShootoutOutcome.None) => new(PenaltyShootoutId.New(), takerId, result);

    public PlayerId? TakerId { get; set; }

    public PenaltyShootoutOutcome Result { get; set; }

    public override string ToString()
    {
        var str = new StringBuilder();

        switch (Result)
        {
            case PenaltyShootoutOutcome.None:
                _ = str.Append('?');
                break;
            case PenaltyShootoutOutcome.Succeeded:
                _ = str.Append('O');
                break;
            case PenaltyShootoutOutcome.Failed:
                _ = str.Append('X');
                break;
            default:
                break;
        }

        if (TakerId is not null)
            _ = str.Append(CultureInfo.CurrentCulture, $" ({TakerId})");

        return str.ToString();
    }
}
