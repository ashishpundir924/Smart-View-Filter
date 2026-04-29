using System.Runtime.Serialization;

namespace SmartViewFilter.Revit.Models
{
    [DataContract]
    public class FilterRule
    {
        [DataMember]
        public string LogicWithPrevious { get; set; } = "AND";

        [DataMember]
        public string ParameterName { get; set; }

        [DataMember]
        public string Operator { get; set; } = "Equals";

        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public bool MatchCase { get; set; }
    }
}
