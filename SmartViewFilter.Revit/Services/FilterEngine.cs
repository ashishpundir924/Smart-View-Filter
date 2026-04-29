using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SmartViewFilter.Revit.Models;

namespace SmartViewFilter.Revit.Services
{
    public class FilterEngine
    {
        public IReadOnlyList<ElementRecord> Filter(
            IEnumerable<ElementRecord> source,
            ISet<string> selectedScopeKeys,
            IReadOnlyList<FilterRule> rules)
        {
            var matches = new List<ElementRecord>();
            foreach (ElementRecord record in source ?? Enumerable.Empty<ElementRecord>())
            {
                if (!IsInScope(record, selectedScopeKeys))
                {
                    continue;
                }

                if (!EvaluateRules(record, rules))
                {
                    continue;
                }

                matches.Add(record);
            }

            return matches;
        }

        private static bool IsInScope(ElementRecord record, ISet<string> selectedScopeKeys)
        {
            if (selectedScopeKeys == null || selectedScopeKeys.Count == 0)
            {
                return true;
            }

            string categoryKey = $"Category|{record.CategoryName}||";
            string familyKey = $"Family|{record.CategoryName}|{record.FamilyName}|";
            string typeKey = $"Type|{record.CategoryName}|{record.FamilyName}|{record.TypeName}";

            return selectedScopeKeys.Contains(categoryKey)
                || selectedScopeKeys.Contains(familyKey)
                || selectedScopeKeys.Contains(typeKey);
        }

        private static bool EvaluateRules(ElementRecord record, IReadOnlyList<FilterRule> rules)
        {
            List<FilterRule> configuredRules = (rules ?? Array.Empty<FilterRule>())
                .Where(IsConfigured)
                .ToList();

            if (configuredRules.Count == 0)
            {
                return true;
            }

            bool result = EvaluateRule(record, configuredRules[0]);
            for (int i = 1; i < configuredRules.Count; i++)
            {
                bool next = EvaluateRule(record, configuredRules[i]);
                string logic = configuredRules[i].LogicWithPrevious ?? "AND";
                result = logic.Equals("OR", StringComparison.OrdinalIgnoreCase)
                    ? result || next
                    : result && next;
            }

            return result;
        }

        private static bool IsConfigured(FilterRule rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.ParameterName))
            {
                return false;
            }

            return rule.Operator == "Has Value"
                || rule.Operator == "Is Empty"
                || !string.IsNullOrWhiteSpace(rule.Value);
        }

        private static bool EvaluateRule(ElementRecord record, FilterRule rule)
        {
            record.Parameters.TryGetValue(rule.ParameterName, out ParameterValue parameterValue);

            string operatorName = string.IsNullOrWhiteSpace(rule.Operator) ? "Equals" : rule.Operator;
            if (operatorName == "Is Empty")
            {
                return parameterValue == null || !parameterValue.HasValue || string.IsNullOrWhiteSpace(parameterValue.Text);
            }

            if (operatorName == "Has Value")
            {
                return parameterValue != null && parameterValue.HasValue && !string.IsNullOrWhiteSpace(parameterValue.Text);
            }

            if (parameterValue == null)
            {
                return false;
            }

            string actual = parameterValue.Text ?? string.Empty;
            string expected = rule.Value ?? string.Empty;
            StringComparison comparison = rule.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            switch (operatorName)
            {
                case "Equals":
                    return NumericCompare(parameterValue, expected, (a, b) => Math.Abs(a - b) < 0.0000001)
                        ?? string.Equals(actual, expected, comparison);
                case "Not Equals":
                    return NumericCompare(parameterValue, expected, (a, b) => Math.Abs(a - b) >= 0.0000001)
                        ?? !string.Equals(actual, expected, comparison);
                case "Contains":
                    return actual.IndexOf(expected, comparison) >= 0;
                case "Does Not Contain":
                    return actual.IndexOf(expected, comparison) < 0;
                case "Starts With":
                    return actual.StartsWith(expected, comparison);
                case "Ends With":
                    return actual.EndsWith(expected, comparison);
                case "Greater Than":
                    return NumericCompare(parameterValue, expected, (a, b) => a > b) ?? false;
                case "Greater Or Equal":
                    return NumericCompare(parameterValue, expected, (a, b) => a >= b) ?? false;
                case "Less Than":
                    return NumericCompare(parameterValue, expected, (a, b) => a < b) ?? false;
                case "Less Or Equal":
                    return NumericCompare(parameterValue, expected, (a, b) => a <= b) ?? false;
                default:
                    return false;
            }
        }

        private static bool? NumericCompare(ParameterValue actualValue, string expectedText, Func<double, double, bool> compare)
        {
            if (!TryReadNumber(actualValue, out double actual))
            {
                return null;
            }

            if (!TryParseDouble(expectedText, out double expected))
            {
                return null;
            }

            return compare(actual, expected);
        }

        private static bool TryReadNumber(ParameterValue value, out double number)
        {
            if (value?.Number != null)
            {
                number = value.Number.Value;
                return true;
            }

            return TryParseDouble(value?.Text, out number);
        }

        private static bool TryParseDouble(string text, out double number)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                || double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out number);
        }
    }
}
