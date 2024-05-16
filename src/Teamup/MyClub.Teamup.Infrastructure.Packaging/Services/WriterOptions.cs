// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Teamup.Infrastructure.Packaging.Services
{
    [Flags]
    public enum WriterOptions
    {
        WriteMetadata = 1,

        WritePlayers = 2,

        WriteSquads = 4,

        WriteTrainingSessions = 8,

        WriteSendedMails = 16,

        WriteHolidays = 32,

        WriteCycles = 64,

        WriteTactics = 128,

        WriteStadiums = 256,

        WriteClubs = 512,

        WriteCompetitions = 1024,

        WriteAll = WriteMetadata | WritePlayers | WriteSquads | WriteTrainingSessions | WriteSendedMails | WriteHolidays | WriteCycles | WriteTactics | WriteStadiums | WriteClubs | WriteCompetitions,
    }
}
