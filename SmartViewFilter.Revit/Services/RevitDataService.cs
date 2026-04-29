using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SmartViewFilter.Revit.Models;
using ElementRecord = SmartViewFilter.Revit.Models.ElementRecord;
using ParameterValue = SmartViewFilter.Revit.Models.ParameterValue;

namespace SmartViewFilter.Revit.Services
{
    public class RevitDataService
    {
        private readonly UIApplication _uiApplication;

        public RevitDataService(UIApplication uiApplication)
        {
            _uiApplication = uiApplication ?? throw new ArgumentNullException(nameof(uiApplication));
        }

        private UIDocument UiDocument => _uiApplication.ActiveUIDocument;
        private Document Document => UiDocument?.Document;

        public IReadOnlyList<ElementRecord> ReadSelectedElements()
        {
            UIDocument uiDocument = UiDocument;
            Document document = Document;
            if (uiDocument == null || document == null)
            {
                return Array.Empty<ElementRecord>();
            }

            ICollection<ElementId> selectedIds = uiDocument.Selection.GetElementIds();
            if (selectedIds == null || selectedIds.Count == 0)
            {
                return Array.Empty<ElementRecord>();
            }

            var records = new List<ElementRecord>();
            foreach (ElementId id in selectedIds)
            {
                Element element = document.GetElement(id);
                if (element == null)
                {
                    continue;
                }

                records.Add(CreateRecord(document, element));
            }

            return records;
        }

        public IReadOnlyList<ElementId> GetSelectedElementIds()
        {
            return UiDocument?.Selection.GetElementIds()?.ToList() ?? new List<ElementId>();
        }

        public HashSet<ElementId> GetVisibleElementIdsInActiveView(bool includeElementTypes)
        {
            Document document = Document;
            View activeView = document?.ActiveView;
            if (document == null || activeView == null)
            {
                return new HashSet<ElementId>();
            }

            FilteredElementCollector collector = new FilteredElementCollector(document, activeView.Id);
            if (!includeElementTypes)
            {
                collector = collector.WhereElementIsNotElementType();
            }

            return new HashSet<ElementId>(collector.ToElementIds());
        }

        public void SelectElements(IEnumerable<ElementId> ids)
        {
            UIDocument uiDocument = UiDocument;
            if (uiDocument == null)
            {
                return;
            }

            uiDocument.Selection.SetElementIds(ids?.Distinct().ToList() ?? new List<ElementId>());
        }

        public void TemporarilyIsolateElements(IEnumerable<ElementId> ids)
        {
            Document document = Document;
            View activeView = document?.ActiveView;
            List<ElementId> elementIds = ids?.Distinct().ToList() ?? new List<ElementId>();
            if (document == null || activeView == null || elementIds.Count == 0)
            {
                return;
            }

            using (var transaction = new Transaction(document, "Smart View Filter - Isolate"))
            {
                transaction.Start();
                activeView.IsolateElementsTemporary(elementIds);
                transaction.Commit();
            }
        }

        internal static ElementRecord CreateRecord(Document document, Element element)
        {
            ElementType elementType = GetElementType(document, element);

            var record = new ElementRecord
            {
                Id = element.Id,
                CategoryName = element.Category?.Name ?? "(No Category)",
                FamilyName = GetFamilyName(element, elementType),
                TypeName = GetTypeName(element, elementType),
                IsElementType = element is ElementType
            };

            AddParameters(record, element);
            if (elementType != null && elementType.Id != element.Id)
            {
                AddParameters(record, elementType);
            }

            AddSyntheticParameter(record, "Category", record.CategoryName);
            AddSyntheticParameter(record, "Family", record.FamilyName);
            AddSyntheticParameter(record, "Type", record.TypeName);
            double elementIdNumber = GetElementIdNumber(record.Id);
            AddSyntheticParameter(record, "Element Id", elementIdNumber.ToString(CultureInfo.InvariantCulture), elementIdNumber);

            return record;
        }

        internal static double GetElementIdNumber(ElementId id)
        {
#if REVIT2026
            return id.Value;
#else
            return id.IntegerValue;
#endif
        }

        private static ElementType GetElementType(Document document, Element element)
        {
            if (element is ElementType currentType)
            {
                return currentType;
            }

            ElementId typeId = element.GetTypeId();
            if (typeId == null || typeId == ElementId.InvalidElementId)
            {
                return null;
            }

            return document.GetElement(typeId) as ElementType;
        }

        private static string GetFamilyName(Element element, ElementType elementType)
        {
            if (element is FamilyInstance familyInstance)
            {
                return familyInstance.Symbol?.FamilyName ?? "(No Family)";
            }

            if (!string.IsNullOrWhiteSpace(elementType?.FamilyName))
            {
                return elementType.FamilyName;
            }

            return element.Category?.Name ?? "(No Family)";
        }

        private static string GetTypeName(Element element, ElementType elementType)
        {
            if (!string.IsNullOrWhiteSpace(elementType?.Name))
            {
                return elementType.Name;
            }

            return string.IsNullOrWhiteSpace(element.Name) ? "(No Type)" : element.Name;
        }

        private static void AddParameters(ElementRecord record, Element element)
        {
            foreach (Parameter parameter in element.Parameters)
            {
                string name = parameter?.Definition?.Name;
                if (string.IsNullOrWhiteSpace(name) || record.Parameters.ContainsKey(name))
                {
                    continue;
                }

                record.Parameters[name] = ReadParameterValue(parameter, name);
            }
        }

        private static ParameterValue ReadParameterValue(Parameter parameter, string name)
        {
            var value = new ParameterValue
            {
                Name = name,
                HasValue = parameter.HasValue
            };

            if (!parameter.HasValue)
            {
                value.Text = string.Empty;
                return value;
            }

            try
            {
                switch (parameter.StorageType)
                {
                    case StorageType.Double:
                        value.Number = parameter.AsDouble();
                        value.Text = parameter.AsValueString();
                        break;
                    case StorageType.Integer:
                        value.Number = parameter.AsInteger();
                        value.Text = parameter.AsValueString();
                        break;
                    case StorageType.ElementId:
                        ElementId id = parameter.AsElementId();
                        value.Number = id == null ? null : GetElementIdNumber(id);
                        value.Text = parameter.AsValueString();
                        break;
                    case StorageType.String:
                        value.Text = parameter.AsString();
                        break;
                    default:
                        value.Text = parameter.AsValueString();
                        break;
                }
            }
            catch
            {
                value.Text = parameter.AsString();
            }

            if (string.IsNullOrWhiteSpace(value.Text))
            {
                value.Text = value.Number?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            }

            return value;
        }

        private static void AddSyntheticParameter(ElementRecord record, string name, string text, double? number = null)
        {
            if (record.Parameters.ContainsKey(name))
            {
                return;
            }

            record.Parameters[name] = new ParameterValue
            {
                Name = name,
                Text = text ?? string.Empty,
                Number = number,
                HasValue = !string.IsNullOrWhiteSpace(text) || number.HasValue
            };
        }
    }
}
