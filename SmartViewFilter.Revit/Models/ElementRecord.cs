using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace SmartViewFilter.Revit.Models
{
    public class ElementRecord
    {
        public ElementId Id { get; set; }
        public string CategoryName { get; set; }
        public string FamilyName { get; set; }
        public string TypeName { get; set; }
        public bool IsElementType { get; set; }
        public Dictionary<string, ParameterValue> Parameters { get; } = new Dictionary<string, ParameterValue>();
    }
}
