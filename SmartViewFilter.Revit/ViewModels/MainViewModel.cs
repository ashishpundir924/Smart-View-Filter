using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using SmartViewFilter.Revit.Infrastructure;
using SmartViewFilter.Revit.Models;
using SmartViewFilter.Revit.Services;
using ElementRecord = SmartViewFilter.Revit.Models.ElementRecord;
using FilterRule = SmartViewFilter.Revit.Models.FilterRule;

namespace SmartViewFilter.Revit.ViewModels
{
    internal class MainViewModel : ViewModelBase
    {
        private readonly Action<Action<IReadOnlyList<ElementRecord>>> _readSelectionRequest;
        private readonly Action<SmartFilterRequest, Action<SmartFilterResult>> _filterRequest;
        private readonly ConfigurationStore _configurationStore;
        private readonly DispatcherTimer _livePreviewTimer;
        private List<ElementRecord> _sourceRecords = new List<ElementRecord>();
        private bool _hasSource;
        private bool _limitToActiveView = true;
        private bool _includeElementTypes;
        private bool _livePreviewEnabled = true;
        private bool _selectedSourceOnly = true;
        private SmartFilterAction _lastAction = SmartFilterAction.Apply;
        private string _scopeSearchText;
        private string _statusText = "Select elements in Revit, then click Read Selection.";
        private string _validationText;
        private string _sourceSummaryText = "No source loaded";
        private string _matchCountText = "No results yet";
        private string _newConfigurationName;
        private SavedConfiguration _selectedConfiguration;

        public MainViewModel(
            Action<Action<IReadOnlyList<ElementRecord>>> readSelectionRequest,
            Action<SmartFilterRequest, Action<SmartFilterResult>> filterRequest,
            ConfigurationStore configurationStore)
        {
            _readSelectionRequest = readSelectionRequest ?? throw new ArgumentNullException(nameof(readSelectionRequest));
            _filterRequest = filterRequest ?? throw new ArgumentNullException(nameof(filterRequest));
            _configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));

            ReadSelectionCommand = new RelayCommand(ReadSelection);
            ExpandAllCommand = new RelayCommand(() => SetExpansion(true));
            CollapseAllCommand = new RelayCommand(() => SetExpansion(false));
            SelectAllScopeCommand = new RelayCommand(() => SetAllScopeChecked(true));
            ClearScopeCommand = new RelayCommand(() => SetAllScopeChecked(false));
            AddRuleCommand = new RelayCommand(AddRule);
            RemoveLastRuleCommand = new RelayCommand(RemoveLastRule);
            ApplyCommand = new RelayCommand(() => ExecuteFilter(SmartFilterAction.Apply));
            SelectCommand = new RelayCommand(() => ExecuteFilter(SmartFilterAction.Select));
            IsolateCommand = new RelayCommand(() => ExecuteFilter(SmartFilterAction.Isolate));
            ClearCommand = new RelayCommand(() => ExecuteFilter(SmartFilterAction.Clear));
            SaveConfigurationCommand = new RelayCommand(SaveConfiguration);
            LoadConfigurationCommand = new RelayCommand(LoadConfiguration);
            DeleteConfigurationCommand = new RelayCommand(DeleteConfiguration);
            OpenPrivacyPolicyCommand = new RelayCommand(OpenPrivacyPolicy);

            _livePreviewTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(350)
            };
            _livePreviewTimer.Tick += (_, _) =>
            {
                _livePreviewTimer.Stop();
                ExecuteFilter(SmartFilterAction.Apply);
            };

            LoadSavedConfigurations();
            AddRule();
        }

        public ObservableCollection<ScopeNode> ScopeNodes { get; } = new ObservableCollection<ScopeNode>();
        public ObservableCollection<string> AvailableParameters { get; } = new ObservableCollection<string>();
        public ObservableCollection<RuleViewModel> Rules { get; } = new ObservableCollection<RuleViewModel>();
        public ObservableCollection<SavedConfiguration> SavedConfigurations { get; } = new ObservableCollection<SavedConfiguration>();

        public ICommand ReadSelectionCommand { get; }
        public ICommand ExpandAllCommand { get; }
        public ICommand CollapseAllCommand { get; }
        public ICommand SelectAllScopeCommand { get; }
        public ICommand ClearScopeCommand { get; }
        public ICommand AddRuleCommand { get; }
        public ICommand RemoveLastRuleCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand SelectCommand { get; }
        public ICommand IsolateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SaveConfigurationCommand { get; }
        public ICommand LoadConfigurationCommand { get; }
        public ICommand DeleteConfigurationCommand { get; }
        public ICommand OpenPrivacyPolicyCommand { get; }

        public bool HasSource
        {
            get => _hasSource;
            set => SetProperty(ref _hasSource, value);
        }

        public bool LimitToActiveView
        {
            get => _limitToActiveView;
            set
            {
                if (SetProperty(ref _limitToActiveView, value))
                {
                    QueueLivePreview();
                }
            }
        }

        public bool IncludeElementTypes
        {
            get => _includeElementTypes;
            set
            {
                if (SetProperty(ref _includeElementTypes, value))
                {
                    QueueLivePreview();
                }
            }
        }

        public bool LivePreviewEnabled
        {
            get => _livePreviewEnabled;
            set
            {
                if (SetProperty(ref _livePreviewEnabled, value))
                {
                    QueueLivePreview();
                }
            }
        }

        public bool SelectedSourceOnly
        {
            get => _selectedSourceOnly;
            set
            {
                if (SetProperty(ref _selectedSourceOnly, value))
                {
                    OnPropertyChanged(nameof(ProjectElements));
                    QueueLivePreview();
                }
            }
        }

        public bool ProjectElements
        {
            get => !SelectedSourceOnly;
            set
            {
                if (value)
                {
                    SelectedSourceOnly = false;
                }
                else
                {
                    OnPropertyChanged(nameof(ProjectElements));
                }
            }
        }

        public string ScopeSearchText
        {
            get => _scopeSearchText;
            set
            {
                if (SetProperty(ref _scopeSearchText, value))
                {
                    ApplyScopeSearch();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string ValidationText
        {
            get => _validationText;
            set => SetProperty(ref _validationText, value);
        }

        public string SourceSummaryText
        {
            get => _sourceSummaryText;
            set => SetProperty(ref _sourceSummaryText, value);
        }

        public string MatchCountText
        {
            get => _matchCountText;
            set => SetProperty(ref _matchCountText, value);
        }

        public string NewConfigurationName
        {
            get => _newConfigurationName;
            set => SetProperty(ref _newConfigurationName, value);
        }

        public SavedConfiguration SelectedConfiguration
        {
            get => _selectedConfiguration;
            set => SetProperty(ref _selectedConfiguration, value);
        }

        private void ReadSelection()
        {
            ValidationText = null;
            StatusText = "Reading selected Revit elements...";
            _readSelectionRequest(LoadSourceRecords);
        }

        private void LoadSourceRecords(IReadOnlyList<ElementRecord> records)
        {
            _sourceRecords = records?.ToList() ?? new List<ElementRecord>();
            ScopeNodes.Clear();
            AvailableParameters.Clear();

            if (_sourceRecords.Count == 0)
            {
                HasSource = false;
                SourceSummaryText = "No source loaded";
                MatchCountText = "No results yet";
                StatusText = "No Revit selection found. Select elements in the model first, then click Read Selection.";
                ValidationText = "Select one or more Revit elements before reading the source.";
                UpdateRuleParameterOptions();
                return;
            }

            BuildScopeTree(_sourceRecords);
            foreach (string parameterName in _sourceRecords
                .SelectMany(record => record.Parameters.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name))
            {
                AvailableParameters.Add(parameterName);
            }

            if (Rules.Count == 0)
            {
                AddRule();
            }

            UpdateRuleParameterOptions();
            HasSource = true;
            SetExpansion(true, firstLevelOnly: true);
            SetAllScopeChecked(true);
            SourceSummaryText = $"{_sourceRecords.Count} selected source elements loaded";
            MatchCountText = "Ready to filter";
            StatusText = "Source loaded. Choose selected-source-only or project scope, add rules, then Apply, Select, or Isolate.";
            QueueLivePreview();
        }

        private void BuildScopeTree(IReadOnlyList<ElementRecord> records)
        {
            foreach (IGrouping<string, ElementRecord> categoryGroup in records
                .GroupBy(record => record.CategoryName)
                .OrderBy(group => group.Key))
            {
                var categoryNode = new ScopeNode(categoryGroup.Key, ScopeNodeKind.Category)
                {
                    CategoryName = categoryGroup.Key,
                    Count = categoryGroup.Count()
                };

                foreach (IGrouping<string, ElementRecord> familyGroup in categoryGroup
                    .GroupBy(record => record.FamilyName)
                    .OrderBy(group => group.Key))
                {
                    var familyNode = new ScopeNode(familyGroup.Key, ScopeNodeKind.Family)
                    {
                        CategoryName = categoryGroup.Key,
                        FamilyName = familyGroup.Key,
                        Count = familyGroup.Count()
                    };

                    foreach (IGrouping<string, ElementRecord> typeGroup in familyGroup
                        .GroupBy(record => record.TypeName)
                        .OrderBy(group => group.Key))
                    {
                        familyNode.AddChild(new ScopeNode(typeGroup.Key, ScopeNodeKind.Type)
                        {
                            CategoryName = categoryGroup.Key,
                            FamilyName = familyGroup.Key,
                            TypeName = typeGroup.Key,
                            Count = typeGroup.Count()
                        });
                    }

                    categoryNode.AddChild(familyNode);
                }

                SubscribeScopeNode(categoryNode);
                ScopeNodes.Add(categoryNode);
            }
        }

        private void ExecuteFilter(SmartFilterAction action)
        {
            ValidationText = null;

            if (action != SmartFilterAction.Clear && _sourceRecords.Count == 0)
            {
                ValidationText = "Read a Revit selection before filtering.";
                StatusText = "No source elements are loaded.";
                MatchCountText = "No results";
                return;
            }

            SmartFilterRequest request = BuildRequest(action);
            _lastAction = action;
            StatusText = action == SmartFilterAction.Apply
                ? "Finding matching elements..."
                : action == SmartFilterAction.Select
                    ? "Selecting matching elements..."
                    : action == SmartFilterAction.Isolate
                        ? "Isolating matching elements..."
                        : "Clearing selection and temporary isolate...";

            _filterRequest(request, ApplyFilterResult);
        }

        private SmartFilterRequest BuildRequest(SmartFilterAction action)
        {
            return new SmartFilterRequest
            {
                Action = action,
                SourceElementIds = _sourceRecords.Select(record => record.Id).ToList(),
                SelectedScopeKeys = CollectSelectedScopeKeys().OrderBy(key => key).ToList(),
                Rules = Rules
                    .Where(rule => rule.IsConfigured)
                    .Select(rule => rule.ToModel())
                    .ToList(),
                IncludeElementTypes = IncludeElementTypes,
                LimitToActiveView = LimitToActiveView,
                SelectedSourceOnly = SelectedSourceOnly
            };
        }

        private void ApplyFilterResult(SmartFilterResult result)
        {
            if (result == null)
            {
                ValidationText = "No result was returned from Revit.";
                return;
            }

            if (result.IsError)
            {
                ValidationText = result.Message;
                StatusText = result.Message;
                return;
            }

            MatchCountText = $"{result.MatchCount} matching elements";
            StatusText = result.Message;

            if (!Rules.Any(rule => rule.IsConfigured) && HasSource)
            {
                ValidationText = "No complete rule is configured. Results are based on source scope only.";
            }

            if (_lastAction == SmartFilterAction.Clear)
            {
                foreach (RuleViewModel rule in Rules)
                {
                    rule.Value = string.Empty;
                }
            }
        }

        private void AddRule()
        {
            var rule = new RuleViewModel
            {
                IsFirst = Rules.Count == 0,
                LogicWithPrevious = "AND",
                Operator = "Contains"
            };

            rule.PropertyChanged += RuleOnPropertyChanged;
            rule.UpdateParameterOptions(AvailableParameters);
            Rules.Add(rule);
            RefreshRuleOrder();
        }

        private void RuleOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RuleViewModel.ParameterSearchText))
            {
                return;
            }

            QueueLivePreview();
        }

        private void RemoveLastRule()
        {
            if (Rules.Count > 0)
            {
                Rules[Rules.Count - 1].PropertyChanged -= RuleOnPropertyChanged;
                Rules.RemoveAt(Rules.Count - 1);
                RefreshRuleOrder();
            }
        }

        private void RefreshRuleOrder()
        {
            for (int i = 0; i < Rules.Count; i++)
            {
                Rules[i].IsFirst = i == 0;
            }
        }

        private void UpdateRuleParameterOptions()
        {
            foreach (RuleViewModel rule in Rules)
            {
                rule.UpdateParameterOptions(AvailableParameters);
            }
        }

        private void QueueLivePreview()
        {
            if (!LivePreviewEnabled || !HasSource)
            {
                return;
            }

            _livePreviewTimer.Stop();
            _livePreviewTimer.Start();
        }

        private void SubscribeScopeNode(ScopeNode node)
        {
            node.CheckedChanged += (_, _) => QueueLivePreview();
            foreach (ScopeNode child in node.Children)
            {
                SubscribeScopeNode(child);
            }
        }

        private void ClearState()
        {
            _sourceRecords.Clear();
            ScopeNodes.Clear();
            AvailableParameters.Clear();
            foreach (RuleViewModel rule in Rules)
            {
                rule.PropertyChanged -= RuleOnPropertyChanged;
            }
            Rules.Clear();
            AddRule();
            HasSource = false;
            ScopeSearchText = null;
            ValidationText = null;
            SourceSummaryText = "No source loaded";
            MatchCountText = "No results yet";
            StatusText = "Cleared. Select elements in Revit, then click Read Selection.";
        }

        private void SetExpansion(bool isExpanded, bool firstLevelOnly = false)
        {
            foreach (ScopeNode node in ScopeNodes)
            {
                SetExpansion(node, isExpanded, firstLevelOnly, level: 0);
            }
        }

        private static void SetExpansion(ScopeNode node, bool isExpanded, bool firstLevelOnly, int level)
        {
            node.IsExpanded = isExpanded && (!firstLevelOnly || level == 0);
            foreach (ScopeNode child in node.Children)
            {
                SetExpansion(child, isExpanded, firstLevelOnly, level + 1);
            }
        }

        private void SetAllScopeChecked(bool isChecked)
        {
            foreach (ScopeNode node in ScopeNodes)
            {
                node.SetChecked(isChecked, updateChildren: true, updateParent: false);
            }
        }

        private HashSet<string> CollectSelectedScopeKeys()
        {
            var keys = new HashSet<string>();
            foreach (ScopeNode node in ScopeNodes)
            {
                CollectSelectedScopeKeys(node, keys);
            }

            return keys;
        }

        private static void CollectSelectedScopeKeys(ScopeNode node, ISet<string> keys)
        {
            if (node.IsChecked)
            {
                keys.Add(node.Key);
            }

            foreach (ScopeNode child in node.Children)
            {
                CollectSelectedScopeKeys(child, keys);
            }
        }

        private void ApplyScopeSearch()
        {
            foreach (ScopeNode node in ScopeNodes)
            {
                ApplyScopeSearch(node, ScopeSearchText);
            }
        }

        private static bool ApplyScopeSearch(ScopeNode node, string searchText)
        {
            bool selfMatches = node.MatchesSearch(searchText);
            bool childMatches = false;

            foreach (ScopeNode child in node.Children)
            {
                childMatches |= ApplyScopeSearch(child, searchText);
            }

            node.IsVisible = selfMatches || childMatches;
            if (!string.IsNullOrWhiteSpace(searchText) && childMatches)
            {
                node.IsExpanded = true;
            }

            return node.IsVisible;
        }

        private void LoadSavedConfigurations()
        {
            SavedConfigurations.Clear();
            foreach (SavedConfiguration configuration in _configurationStore.Load().OrderBy(config => config.Name))
            {
                SavedConfigurations.Add(configuration);
            }
        }

        private void SaveConfiguration()
        {
            ValidationText = null;
            string name = NewConfigurationName?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ValidationText = "Enter a configuration name before saving.";
                return;
            }

            var configuration = new SavedConfiguration
            {
                Name = name,
                LimitToActiveView = LimitToActiveView,
                IncludeElementTypes = IncludeElementTypes,
                SelectedSourceOnly = SelectedSourceOnly,
                ScopeKeys = CollectSelectedScopeKeys().OrderBy(key => key).ToList(),
                Rules = Rules.Select(rule => rule.ToModel()).ToList()
            };

            List<SavedConfiguration> configurations = SavedConfigurations.ToList();
            configurations.RemoveAll(config => string.Equals(config.Name, name, StringComparison.OrdinalIgnoreCase));
            configurations.Add(configuration);
            _configurationStore.Save(configurations.OrderBy(config => config.Name).ToList());

            NewConfigurationName = null;
            LoadSavedConfigurations();
            SelectedConfiguration = SavedConfigurations.FirstOrDefault(config => config.Name == name);
            StatusText = $"Configuration saved: {name}";
        }

        private void LoadConfiguration()
        {
            ValidationText = null;
            SavedConfiguration configuration = SelectedConfiguration;
            if (configuration == null)
            {
                ValidationText = "Choose a saved configuration to load.";
                return;
            }

            LimitToActiveView = configuration.LimitToActiveView;
            IncludeElementTypes = configuration.IncludeElementTypes;
            SelectedSourceOnly = configuration.SelectedSourceOnly;

            foreach (RuleViewModel rule in Rules)
            {
                rule.PropertyChanged -= RuleOnPropertyChanged;
            }
            Rules.Clear();

            foreach (FilterRule rule in configuration.Rules)
            {
                RuleViewModel ruleViewModel = RuleViewModel.FromModel(rule, Rules.Count == 0);
                ruleViewModel.PropertyChanged += RuleOnPropertyChanged;
                ruleViewModel.UpdateParameterOptions(AvailableParameters);
                Rules.Add(ruleViewModel);
            }

            if (Rules.Count == 0)
            {
                AddRule();
            }

            RefreshRuleOrder();
            SetAllScopeChecked(false);
            HashSet<string> scopeKeys = new HashSet<string>(configuration.ScopeKeys ?? new List<string>());
            foreach (ScopeNode node in ScopeNodes)
            {
                ApplySavedScope(node, scopeKeys);
            }

            StatusText = $"Configuration loaded: {configuration.Name}";
        }

        private static void ApplySavedScope(ScopeNode node, ISet<string> scopeKeys)
        {
            node.SetChecked(scopeKeys.Contains(node.Key), updateChildren: false, updateParent: false);
            foreach (ScopeNode child in node.Children)
            {
                ApplySavedScope(child, scopeKeys);
            }

            node.RefreshCheckedFromChildren();
        }

        private void DeleteConfiguration()
        {
            ValidationText = null;
            SavedConfiguration configuration = SelectedConfiguration;
            if (configuration == null)
            {
                ValidationText = "Choose a saved configuration to delete.";
                return;
            }

            List<SavedConfiguration> configurations = SavedConfigurations.ToList();
            configurations.RemoveAll(config => string.Equals(config.Name, configuration.Name, StringComparison.OrdinalIgnoreCase));
            _configurationStore.Save(configurations);
            LoadSavedConfigurations();
            StatusText = $"Configuration deleted: {configuration.Name}";
        }

        private void OpenPrivacyPolicy()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Constants.PrivacyPolicyUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                ValidationText = "Privacy policy URL: " + Constants.PrivacyPolicyUrl;
            }
        }
    }
}
