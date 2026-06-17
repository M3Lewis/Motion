using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Motion.Utils.MetroTile
{
    /// <summary>
    /// Parses user text rules into strongly typed <see cref="TileRule"/> objects.
    /// </summary>
    public static class RuleParser
    {
        private static readonly Regex RuleRegex = new Regex(
            @"^([LMSE])(?:[(]([\d,]+)[)])?\s+\{([\d\*]+);([\d\*]+)\}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// Parses the input rule strings.
        /// </summary>
        /// <param name="inputRules">Raw rule strings from Grasshopper input.</param>
        /// <returns>A parsed rule list.</returns>
        /// <exception cref="RuleParseException">Thrown when a rule has invalid format or invalid indices.</exception>
        public static List<TileRule> ParseRules(List<string> inputRules)
        {
            var rules = new List<TileRule>();
            if (inputRules == null || inputRules.Count == 0)
            {
                return rules;
            }

            for (int i = 0; i < inputRules.Count; i++)
            {
                string raw = inputRules[i] ?? string.Empty;
                string text = raw.Trim();

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                Match match = RuleRegex.Match(text);
                if (!match.Success)
                {
                    throw new RuleParseException($"Rule {i + 1} is invalid: \"{raw}\".");
                }

                TileType type = ParseType(match.Groups[1].Value, i, raw);
                HashSet<int> indices = ParseIndices(type, match.Groups[2], i, raw);
                int? column = ParseAxis(match.Groups[3].Value, "column", i, raw);
                int? row = ParseAxis(match.Groups[4].Value, "row", i, raw);

                rules.Add(new TileRule(type, column, row, indices));
            }

            return rules;
        }

        private static TileType ParseType(string token, int ruleIndex, string rawRule)
        {
            switch (token.ToUpperInvariant())
            {
                case "L":
                    return TileType.L;
                case "M":
                    return TileType.M;
                case "S":
                    return TileType.S;
                case "E":
                    return TileType.E;
                default:
                    throw new RuleParseException($"Rule {ruleIndex + 1} has unknown tile type: \"{rawRule}\".");
            }
        }

        private static int? ParseAxis(string token, string axisName, int ruleIndex, string rawRule)
        {
            if (token == "*")
            {
                return null;
            }

            if (!int.TryParse(token, out int value) || value < 0)
            {
                throw new RuleParseException(
                    $"Rule {ruleIndex + 1} has invalid {axisName} value \"{token}\": \"{rawRule}\".");
            }

            return value;
        }

        private static HashSet<int> ParseIndices(TileType type, Group indexGroup, int ruleIndex, string rawRule)
        {
            if (!indexGroup.Success || string.IsNullOrWhiteSpace(indexGroup.Value))
            {
                return null;
            }

            var parsed = new HashSet<int>();
            string[] parts = indexGroup.Value.Split(',');

            for (int p = 0; p < parts.Length; p++)
            {
                string token = parts[p].Trim();
                if (!int.TryParse(token, out int index))
                {
                    throw new RuleParseException(
                        $"Rule {ruleIndex + 1} has non-numeric index \"{token}\": \"{rawRule}\".");
                }

                if (!IsIndexAllowed(type, index))
                {
                    throw new RuleParseException(
                        $"Rule {ruleIndex + 1} has out-of-range index {index} for tile type {type}: \"{rawRule}\".");
                }

                parsed.Add(index);
            }

            if ((type == TileType.L || type == TileType.E) && parsed.Count > 0)
            {
                throw new RuleParseException(
                    $"Rule {ruleIndex + 1} must not provide indices for tile type {type}: \"{rawRule}\".");
            }

            return parsed;
        }

        private static bool IsIndexAllowed(TileType type, int index)
        {
            switch (type)
            {
                case TileType.M:
                    return index >= 0 && index <= 1;
                case TileType.S:
                    return index >= 0 && index <= 3;
                case TileType.L:
                case TileType.E:
                    return false;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Represents parsing errors for metro tile rules.
    /// </summary>
    public sealed class RuleParseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuleParseException"/> class.
        /// </summary>
        /// <param name="message">Error message.</param>
        public RuleParseException(string message)
            : base(message)
        {
        }
    }
}
