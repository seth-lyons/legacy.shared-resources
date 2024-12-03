using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{
    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class EmailLog
    {
        [XmlAttribute("LogId")]
        public long ID { get; set; }

        [XmlAttribute("EmailTemplateId")]
        public int TemplateId { get; set; }

        [XmlAttribute("EmailTemplateName")]
        public string TemplateName { get; set; }

        [XmlAttribute("AgentId")]
        public int AgentId { get; set; }

        [XmlAttribute("AgentName")]
        public string AgentName { get; set; }

        [XmlAttribute("SendDate")]
        public string LogDateText
        {
            get => LogDate?.ToString();
            set { LogDate = DateTime.TryParse(value, out DateTime d) ? (DateTime?)d : null; }
        }
        public DateTime? LogDate { get; set; }
    }
}
