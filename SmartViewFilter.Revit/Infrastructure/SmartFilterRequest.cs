using System.Collections.Generic;
using Autodesk.Revit.DB;
using FilterRule = SmartViewFilter.Revit.Models.FilterRule;

namespace SmartViewFilter.Revit.Infrastructure
{
    internal sealed class SmartFilterRequest
    {
        public SmartFilterAction Action { get; set; }

        public List<ElementId> SourceElementIds { get; set; } = new List<ElementId>();

        public List<string> SelectedScopeKeys { get; set; } = new List<string>();

        public List<FilterRule> Rules { get; set; } = new List<FilterRule>();

        public bool IncludeElementTypes { get; set; }

        public bool LimitToActiveView { get; set; } = true;

        public bool SelectedSourceOnly { get; set; } = true;
    }
}
