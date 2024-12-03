using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{
    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class SyncLog
    {
        [XmlAttribute("LogId")]
        public long ID { get; set; }

        [XmlAttribute("Result")]
        public string Result { get; set; }

        [XmlAttribute("Message")]
        public string Message { get; set; }

        [XmlAttribute("AgentId")]
        public int AgentId { get; set; }

        [XmlAttribute("AgentName")]
        public string AgentName { get; set; }

        [XmlAttribute("AgentEmail")]
        public string AgentEmail { get; set; }

        [XmlAttribute("LogDate")]
        public string LogDateText
        {
            get => LogDate?.ToString();
            set { LogDate = DateTime.TryParse(value, out DateTime d) ? (DateTime?)d : null; }
        }
        public DateTime? LogDate { get; set; }
    }
}
