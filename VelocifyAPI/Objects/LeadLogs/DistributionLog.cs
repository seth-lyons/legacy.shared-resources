using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{
    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class DistributionLog
    {
        [XmlAttribute("LogId")]
        public long ID { get; set; }

        [XmlAttribute("DistributionProgramId")]
        public int ProgramId { get; set; }

        [XmlAttribute("DistributionProgramName")]
        public string ProgramName { get; set; }

        [XmlAttribute("AssignedAgentId")]
        public int AssignedAgentId { get; set; }

        [XmlAttribute("AssignedAgentName")]
        public string AssignedAgentName { get; set; }

        [XmlAttribute("AssignedAgentEmail")]
        public string AssignedAgentEmail { get; set; }

        [XmlAttribute("AssignedGroupId")]
        public int AssignedGroupId { get; set; }

        [XmlAttribute("AssignedGroupName")]
        public string AssignedGroupName { get; set; }

        [XmlAttribute("LogDate")]
        public string LogDateText
        {
            get => LogDate?.ToString();
            set { LogDate = DateTime.TryParse(value, out DateTime d) ? (DateTime?)d : null; }
        }
        public DateTime? LogDate { get; set; }
    }
}
