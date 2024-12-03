using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{
    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class StatusLog
    {
        [XmlAttribute("LogId")]
        public long ID { get; set; }

        [XmlAttribute("StatusId")]
        public int StatusId { get; set; }

        [XmlAttribute("StatusTitle")]
        public string StatusTitle { get; set; }

        [XmlAttribute("AgentId")]
        public int AgentId { get; set; }

        [XmlAttribute("AgentName")]
        public string AgentName { get; set; }

        [XmlAttribute("AgentEmail")]
        public string AgentEmail { get; set; }

        [XmlAttribute("GroupId")]
        public int GroupId { get; set; }

        [XmlAttribute("GroupName")]
        public string GroupName { get; set; }

        [XmlAttribute("LogDate")]
        public string LogDateText
        {
            get => LogDate?.ToString();
            set { LogDate = DateTime.TryParse(value, out DateTime d) ? (DateTime?)d : null; }
        }
        public DateTime? LogDate { get; set; }
    }
}
