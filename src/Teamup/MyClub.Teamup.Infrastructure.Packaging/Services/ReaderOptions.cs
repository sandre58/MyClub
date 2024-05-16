// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Teamup.Infrastructure.Packaging.Services
{
    [Flags]
    public enum ReaderOptions
    {
        ReadMetadata = 1,

        ReadPlayers = 2,

        ReadSquads = 4,

        ReadTrainingSessions = 8,

        ReadSendedMails = 16,

        ReadHolidays = 32,

        ReadCycles = 64,

        ReadTactics = 128,

        ReadStadiums = 256,

        ReadClubs = 512,

        ReadCompetitions = 1024,

        ReadAll = ReadMetadata | ReadPlayers | ReadSquads | ReadTrainingSessions | ReadSendedMails | ReadHolidays | ReadCycles | ReadTactics | ReadStadiums | ReadClubs | ReadCompetitions,
    }
}
