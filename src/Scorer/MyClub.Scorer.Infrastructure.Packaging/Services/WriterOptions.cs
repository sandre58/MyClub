// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Scorer.Infrastructure.Packaging.Services
{
    [Flags]
    public enum WriterOptions
    {
        WriteMetadata = 1,

        WriteStadiums = 2,

        WriteTeams = 4,

        WriteAll = WriteMetadata | WriteStadiums | WriteTeams,
    }
}
