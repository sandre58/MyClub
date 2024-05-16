// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Scorer.Infrastructure.Packaging.Services
{
    [Flags]
    public enum ReaderOptions
    {
        ReadMetadata = 1,
        ReadStadiums = 2,
        ReadTeams = 4,
        ReadAll = ReadMetadata | ReadStadiums | ReadTeams,
    }
}
