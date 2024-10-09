// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyNet.Humanizer;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Factories
{
    public static partial class StageNamesFactory
    {
        private const string VariableRegexKeyword = "variable";
        private const string OptionsRegexKeyword = "options";
        private const string IndexVariableName = "index";
        private const string DateVariableName = "date";
        private const string RoundVariableName = "round";
        private const string RoundAbbrVariableName = "roundAbbr";
        private const string OrdinalizeOptionName = "R";

        public static string ComputePattern(string pattern, int index, IMatchesStage matchesStage)
        {
            if (string.IsNullOrEmpty(pattern)) return string.Empty;

            var result = PatternRegex().Replace(pattern, match =>
            {
                var value = match.Value.Replace("{", string.Empty).Replace("}", string.Empty);
                return VariableRegex().Replace(value, match1 =>
                {
                    var variable = match1.Groups.ContainsKey(VariableRegexKeyword) ? match1.Groups[VariableRegexKeyword].Value : string.Empty;
                    var options = match1.Groups.ContainsKey(OptionsRegexKeyword) ? match1.Groups[OptionsRegexKeyword].Value : string.Empty;

                    return variable switch
                    {
                        IndexVariableName => options == OrdinalizeOptionName ? index.Ordinalize().OrEmpty() : index.ToString(options).OrEmpty(),
                        DateVariableName => matchesStage.Date.ToString(options, CultureInfo.CurrentCulture),
                        RoundVariableName => MyClubResources.ResourceManager.GetString($"RoundOf{matchesStage.GetAllMatches().Count()}") ?? index.ToString(MyClubResources.RoundX),
                        RoundAbbrVariableName => MyClubResources.ResourceManager.GetString($"RoundOf{matchesStage.GetAllMatches().Count()}Abbr") ?? index.ToString(MyClubResources.RoundXAbbr),
                        _ => string.Empty,
                    };
                });
            });

            return result;
        }

        [GeneratedRegex(@"\{[^\{\}]*\}")]
        private static partial Regex PatternRegex();

        [GeneratedRegex($"(?<{VariableRegexKeyword}>\\w*)(:(?<{OptionsRegexKeyword}>.*))?")]
        private static partial Regex VariableRegex();
    }
}

