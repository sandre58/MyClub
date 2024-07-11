// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using MyNet.Utilities;
using MyNet.Humanizer;

namespace MyClub.Scorer.Domain.Factories
{
    public static partial class StageNamesFactory
    {
        private const string VariableRegexKeyword = "variable";
        private const string OptionsRegexKeyword = "options";
        private const string IndexVariableName = "index";
        private const string DateVariableName = "date";
        private const string OrdinalizeOptionName = "R";

        public static string ComputePattern(string pattern, int index, DateTime date)
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
                        DateVariableName => date.ToString(options, CultureInfo.CurrentCulture),
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

