// -----------------------------------------------------------------------
// <copyright file="QualificationFeedRef.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Unique qualification feed identity for diagnostics.
/// </summary>
public sealed record QualificationFeedRef(StageId SourceStageId, int PathOrder);
