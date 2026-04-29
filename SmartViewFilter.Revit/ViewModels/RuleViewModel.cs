using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SmartViewFilter.Revit.Models;

namespace SmartViewFilter.Revit.ViewModels
{
    public class RuleViewModel : ViewModelBase
    {
        private string _logicWithPrevious = "AND";
        private string _parameterName;
        private string _operator = "Equals";
        private string _value;
        private bool _matchCase;
        private bool _isFirst;
        private string _parameterSearchText;
        private List<string> _allParameterOptions = new List<string>();

        public static IReadOnlyList<string> Operators { get; } = new[]
        {
            "Equals",
            "Not Equals",
            "Contains",
            "Does Not Contain",
            "Starts With",
            "Ends With",
            "Greater Than",
            "Greater Or Equal",
            "Less Than",
            "Less Or Equal",
            "Has Value",
            "Is Empty"
        };

        public static IReadOnlyList<string> LogicOptions { get; } = new[] { "AND", "OR" };

        public IReadOnlyList<string> AvailableOperators => Operators;
        public IReadOnlyList<string> AvailableLogicOptions => LogicOptions;
        public ObservableCollection<string> ParameterOptions { get; } = new ObservableCollection<string>();

        public string LogicWithPrevious
        {
            get => _logicWithPrevious;
            set => SetProperty(ref _logicWithPrevious, value);
        }

        public string ParameterName
        {
            get => _parameterName;
            set => SetProperty(ref _parameterName, value);
        }

        public string ParameterSearchText
        {
            get => _parameterSearchText;
            set
            {
                if (SetProperty(ref _parameterSearchText, value))
                {
                    RefreshFilteredParameters();
                }
            }
        }

        public string Operator
        {
            get => _operator;
            set => SetProperty(ref _operator, value);
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public bool MatchCase
        {
            get => _matchCase;
            set => SetProperty(ref _matchCase, value);
        }

        public bool IsFirst
        {
            get => _isFirst;
            set => SetProperty(ref _isFirst, value);
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ParameterName)
            && (Operator == "Has Value" || Operator == "Is Empty" || !string.IsNullOrWhiteSpace(Value));

        public void UpdateParameterOptions(IEnumerable<string> parameterNames)
        {
            _allParameterOptions = (parameterNames ?? Enumerable.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();

            if (string.IsNullOrWhiteSpace(ParameterName)
                || !_allParameterOptions.Any(name => string.Equals(name, ParameterName, System.StringComparison.OrdinalIgnoreCase)))
            {
                ParameterName = _allParameterOptions.FirstOrDefault();
            }

            RefreshFilteredParameters();
        }

        private void RefreshFilteredParameters()
        {
            string search = ParameterSearchText;
            List<string> filtered = string.IsNullOrWhiteSpace(search)
                ? _allParameterOptions
                : _allParameterOptions
                    .Where(name => name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            ParameterOptions.Clear();
            foreach (string parameter in filtered)
            {
                ParameterOptions.Add(parameter);
            }
        }

        public FilterRule ToModel()
        {
            return new FilterRule
            {
                LogicWithPrevious = LogicWithPrevious,
                ParameterName = ParameterName,
                Operator = Operator,
                Value = Value,
                MatchCase = MatchCase
            };
        }

        public static RuleViewModel FromModel(FilterRule rule, bool isFirst)
        {
            return new RuleViewModel
            {
                LogicWithPrevious = string.IsNullOrWhiteSpace(rule?.LogicWithPrevious) ? "AND" : rule.LogicWithPrevious,
                ParameterName = rule?.ParameterName,
                Operator = string.IsNullOrWhiteSpace(rule?.Operator) ? "Equals" : rule.Operator,
                Value = rule?.Value,
                MatchCase = rule?.MatchCase ?? false,
                IsFirst = isFirst
            };
        }
    }
}
