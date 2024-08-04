// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;

namespace MyClub.Teamup.Domain.Factories
{
    public static class TeamFactory
    {
        public static string GetTeamName(string baseName, IEnumerable<string> existingTeamNames)
            => !existingTeamNames.Contains(baseName, StringComparer.OrdinalIgnoreCase)
                ? baseName
                : $"{baseName} ".IncrementAlpha(existingTeamNames, 2);
    }
}
