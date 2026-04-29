using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SmartViewFilter.Revit.Models
{
    [DataContract]
    public class SavedConfiguration
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool LimitToActiveView { get; set; }

        [DataMember]
        public bool IncludeElementTypes { get; set; }

        [DataMember]
        public bool SelectedSourceOnly { get; set; } = true;

        [DataMember]
        public List<string> ScopeKeys { get; set; } = new List<string>();

        [DataMember]
        public List<FilterRule> Rules { get; set; } = new List<FilterRule>();

        public override string ToString()
        {
            return Name;
        }
    }
}
