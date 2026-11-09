// -----------------------------------------------------------------------
// <copyright file="CascadeReplaceVersion.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages.CascadeReplace;

/// <summary>
/// Fingerprint of the stages and matches a cascade confirmation is allowed to rewrite.
/// </summary>
internal static class CascadeReplaceVersion
{
    public static string Compute(IReadOnlyList<Stage> stages, IReadOnlyList<Match> matches)
    {
        var builder = new StringBuilder();
        foreach (var stage in stages.OrderBy(stage => stage.Id.Value))
        {
            builder.Append(stage.Id.Value.ToString("N")).Append('|').Append(stage.Status).Append('\n');
            foreach (var slot in stage.Slots.OrderBy(slot => slot.SlotKey, StringComparer.Ordinal))
            {
                builder.Append("s|").Append(slot.SlotKey).Append('|')
                    .Append(slot.EntryId?.Value.ToString("N") ?? string.Empty).Append('\n');
            }

            foreach (var group in stage.Groups.OrderBy(group => group.Id.Value))
            {
                builder.Append("g|").Append(group.Id.Value.ToString("N"));
                foreach (var entry in group.EntryIds.OrderBy(entry => entry.Value))
                {
                    builder.Append('|').Append(entry.Value.ToString("N"));
                }

                builder.Append('\n');
            }

            foreach (var entry in stage.CompositionEntries.OrderBy(entry => entry.EntryId.Value))
            {
                builder.Append("p|").Append(entry.EntryId.Value.ToString("N")).Append('\n');
            }
        }

        foreach (var match in matches.OrderBy(match => match.Id.Value))
        {
            builder.Append(match.Id.Value.ToString("N")).Append('|')
                .Append(match.Status).Append('|')
                .Append(match.HomeEntryId?.Value.ToString("N") ?? string.Empty).Append('|')
                .Append(match.AwayEntryId?.Value.ToString("N") ?? string.Empty).Append('|')
                .Append(match.Result?.Type.ToString() ?? string.Empty).Append('|')
                .Append(match.Result?.Score.HomeGoals.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append('|')
                .Append(match.Result?.Score.AwayGoals.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append('|')
                .Append(match.DeclaredParticipations.Count).Append('|')
                .Append(match.RecordedGoals.Count).Append('|')
                .Append(match.RecordedSubstitutions.Count).Append('|')
                .Append(match.RecordedDisciplinaryEvents.Count).Append('\n');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash);
    }
}
