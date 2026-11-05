// -----------------------------------------------------------------------
// <copyright file="ResolutionProvenanceConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Infrastructure.Persistence.Configurations;

internal static class ResolutionProvenanceConfiguration
{
    internal const int SourcePathIdMaxLength = 512;

    internal const int UpstreamFingerprintMaxLength = 2048;

    internal static void MapOptional(
        OwnedNavigationBuilder<Slot, ResolutionProvenance> provenance)
    {
        provenance.Property(p => p.Kind)
            .HasColumnName("resolution_provenance_kind")
            .HasConversion<int>()
            .IsRequired();

        provenance.Property(p => p.SourcePathId)
            .HasColumnName("resolution_source_path_id")
            .HasMaxLength(SourcePathIdMaxLength)
            .IsRequired();

        provenance.Property(p => p.UpstreamResultFingerprint)
            .HasColumnName("resolution_upstream_fingerprint")
            .HasMaxLength(UpstreamFingerprintMaxLength)
            .IsRequired();
    }

    internal static void MapOptional(
        OwnedNavigationBuilder<CompositionEntry, ResolutionProvenance> provenance)
    {
        provenance.Property(p => p.Kind)
            .HasColumnName("resolution_provenance_kind")
            .HasConversion<int>()
            .IsRequired();

        provenance.Property(p => p.SourcePathId)
            .HasColumnName("resolution_source_path_id")
            .HasMaxLength(SourcePathIdMaxLength)
            .IsRequired();

        provenance.Property(p => p.UpstreamResultFingerprint)
            .HasColumnName("resolution_upstream_fingerprint")
            .HasMaxLength(UpstreamFingerprintMaxLength)
            .IsRequired();
    }
}
