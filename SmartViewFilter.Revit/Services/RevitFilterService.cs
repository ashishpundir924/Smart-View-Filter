using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SmartViewFilter.Revit.Infrastructure;
using SmartViewFilter.Revit.Models;
using ElementRecord = SmartViewFilter.Revit.Models.ElementRecord;
using FilterRule = SmartViewFilter.Revit.Models.FilterRule;

namespace SmartViewFilter.Revit.Services
{
    internal sealed class RevitFilterService
    {
        private readonly FilterEngine _filterEngine;

        public RevitFilterService(FilterEngine filterEngine)
        {
            _filterEngine = filterEngine;
        }

        public SmartFilterResult Execute(UIApplication uiApplication, SmartFilterRequest request)
        {
            UIDocument uiDocument = uiApplication.ActiveUIDocument;
            Document document = uiDocument?.Document;
            View activeView = document?.ActiveView;

            if (uiDocument == null || document == null || activeView == null)
            {
                return SmartFilterResult.Error("Open a Revit document before filtering.");
            }

            if (request.Action == SmartFilterAction.Clear)
            {
                uiDocument.Selection.SetElementIds(new List<ElementId>());
                if (activeView.IsTemporaryHideIsolateActive())
                {
                    using (var transaction = new Transaction(document, "Smart View Filter - Clear Temporary Isolate"))
                    {
                        transaction.Start();
                        activeView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                        transaction.Commit();
                    }
                }

                return SmartFilterResult.Success(0, "Selection and temporary isolate cleared.");
            }

            IList<ElementId> matchingIds = FindMatchingElementIds(document, activeView, request);

            if (request.Action == SmartFilterAction.Select)
            {
                uiDocument.Selection.SetElementIds(matchingIds);
                return SmartFilterResult.Success(matchingIds.Count, $"{matchingIds.Count} matching elements selected.");
            }

            if (request.Action == SmartFilterAction.Isolate)
            {
                if (matchingIds.Count == 0)
                {
                    return SmartFilterResult.Error("No matching elements were found to isolate.");
                }

                using (var transaction = new Transaction(document, "Smart View Filter - Isolate"))
                {
                    transaction.Start();
                    activeView.IsolateElementsTemporary(matchingIds);
                    transaction.Commit();
                }

                uiDocument.Selection.SetElementIds(matchingIds);
                return SmartFilterResult.Success(matchingIds.Count, $"{matchingIds.Count} matching elements isolated.");
            }

            return SmartFilterResult.Success(matchingIds.Count, $"{matchingIds.Count} matching elements found.");
        }

        private IList<ElementId> FindMatchingElementIds(Document document, View activeView, SmartFilterRequest request)
        {
            IEnumerable<Element> domain = request.SelectedSourceOnly
                ? ReadSourceElements(document, request.SourceElementIds)
                : ReadProjectElements(document, activeView, request);

            if (!request.IncludeElementTypes)
            {
                domain = domain.Where(element => !(element is ElementType));
            }

            if (request.SelectedSourceOnly && request.LimitToActiveView)
            {
                HashSet<ElementId> visibleIds = new HashSet<ElementId>(
                    new FilteredElementCollector(document, activeView.Id)
                        .WhereElementIsNotElementType()
                        .ToElementIds());

                domain = domain.Where(element => visibleIds.Contains(element.Id));
            }

            var records = new List<ElementRecord>();
            foreach (Element element in domain)
            {
                if (element?.Category == null)
                {
                    continue;
                }

                records.Add(RevitDataService.CreateRecord(document, element));
            }

            HashSet<string> selectedScopeKeys = new HashSet<string>(request.SelectedScopeKeys ?? new List<string>());
            List<FilterRule> rules = request.Rules ?? new List<FilterRule>();
            return _filterEngine
                .Filter(records, selectedScopeKeys, rules)
                .Select(record => record.Id)
                .Distinct()
                .ToList();
        }

        private static IEnumerable<Element> ReadSourceElements(Document document, IEnumerable<ElementId> sourceElementIds)
        {
            foreach (ElementId id in sourceElementIds ?? Enumerable.Empty<ElementId>())
            {
                Element element = document.GetElement(id);
                if (element != null)
                {
                    yield return element;
                }
            }
        }

        private static IEnumerable<Element> ReadProjectElements(Document document, View activeView, SmartFilterRequest request)
        {
            FilteredElementCollector collector = request.LimitToActiveView
                ? new FilteredElementCollector(document, activeView.Id)
                : new FilteredElementCollector(document);

            if (!request.IncludeElementTypes)
            {
                collector = collector.WhereElementIsNotElementType();
            }

            return collector;
        }
    }
}
