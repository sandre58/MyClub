// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Utilities;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.TacticAggregate;

namespace MyClub.Teamup.Domain.Factories.Extensions
{
    public static class TacticExtensions
    {
        public static Tactic CreateTactic(this KnownTactic knownTactic)
        {
            var tactic = new Tactic(MyClubEnumsResources.ResourceManager.GetString(knownTactic.ResourceKey).OrEmpty());
            knownTactic.GetPositions().ForEach(x => tactic.AddPosition(x));

            return tactic;
        }
    }
}
