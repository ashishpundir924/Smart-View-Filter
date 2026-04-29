using System;
using System.Collections.ObjectModel;
using System.Linq;
using SmartViewFilter.Revit.ViewModels;

namespace SmartViewFilter.Revit.Models
{
    public enum ScopeNodeKind
    {
        Category,
        Family,
        Type
    }

    public class ScopeNode : ViewModelBase
    {
        private bool _isChecked;
        private bool _isExpanded;
        private bool _isVisible = true;
        private bool _isUpdating;

        public ScopeNode(string name, ScopeNodeKind kind)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "(None)" : name;
            Kind = kind;
        }

        public string Name { get; }
        public ScopeNodeKind Kind { get; }
        public string CategoryName { get; set; }
        public string FamilyName { get; set; }
        public string TypeName { get; set; }
        public int Count { get; set; }
        public ScopeNode Parent { get; private set; }
        public ObservableCollection<ScopeNode> Children { get; } = new ObservableCollection<ScopeNode>();

        public string DisplayName => Count > 0 ? $"{Name} ({Count})" : Name;

        public string Key => $"{Kind}|{CategoryName}|{FamilyName}|{TypeName}";

        public event EventHandler CheckedChanged;

        public bool IsChecked
        {
            get => _isChecked;
            set => SetChecked(value, updateChildren: true, updateParent: true);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public void AddChild(ScopeNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public void SetChecked(bool value, bool updateChildren, bool updateParent)
        {
            if (_isUpdating)
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
                return;
            }

            _isUpdating = true;
            _isChecked = value;
            OnPropertyChanged(nameof(IsChecked));
            CheckedChanged?.Invoke(this, EventArgs.Empty);

            if (updateChildren)
            {
                foreach (ScopeNode child in Children)
                {
                    child.SetChecked(value, updateChildren: true, updateParent: false);
                }
            }

            _isUpdating = false;

            if (updateParent)
            {
                Parent?.RefreshCheckedFromChildren();
            }
        }

        public void RefreshCheckedFromChildren()
        {
            if (!Children.Any())
            {
                return;
            }

            bool allChildrenChecked = Children.All(child => child.IsChecked);
            if (_isChecked != allChildrenChecked)
            {
                _isChecked = allChildrenChecked;
                OnPropertyChanged(nameof(IsChecked));
            }

            Parent?.RefreshCheckedFromChildren();
        }

        public bool MatchesSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            return DisplayName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
