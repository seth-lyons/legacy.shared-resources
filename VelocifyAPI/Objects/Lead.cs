using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{

    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true), XmlRoot(ElementName = "Leads", Namespace = "", IsNullable = false)]
    public partial class LeadList
    {
        [XmlElement("Lead")]
        public Lead[] Leads { get; set; }
    }

    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class Lead
    {
        [XmlAttribute("Id")]
        public long ID { get; set; }

        [XmlAttribute("LeadTitle")]
        public string Title { get; set; }

        [XmlAttribute("ActionCount")]
        public int ActionCount { get; set; }

        [XmlAttribute("LogCount")]
        public int LogCount { get; set; }

        [XmlAttribute("ReminderCount")]
        public int ReminderCount { get; set; }

        [XmlAttribute("ReadOnly")]
        public bool ReadOnly { get; set; }

        [XmlAttribute("Flagged")]
        public bool Flagged { get; set; }

        [XmlAttribute("LeadFormType")]
        public string LeadFormType { get; set; }

        [XmlElement("Campaign")]
        public Campaign Campaign { get; set; }

        [XmlElement("Status")]
        public Status Status { get; set; }

        [XmlElement("Agent")]
        public Agent Agent { get; set; }

        [XmlArray("Fields"), XmlArrayItem("Field", IsNullable = true)]
        public Field[] Fields { get; set; }

        [XmlAttribute("LastDistributionDate")]
        public string LastDistributionDateText
        {
            get => LastDistributionDate?.ToString();
            set { LastDistributionDate = DateTime.TryParse(value, out DateTime d) ? (DateTime?)d : null; }
        }
        public DateTime? LastDistributionDate { get; set; }


        [XmlAttribute("CreateDate")]
        public string CreatedText
        {
            get => Created?.ToString();
            set { Created = DateTime.TryParse(value, out DateTime d) ? (DateTime?)d : null; }
        }
        public DateTime? Created { get; set; }


        [XmlAttribute("ModifyDate")]
        public string ModifiedText
        {
            get => Modified?.ToString();
            set { Modified = DateTime.TryParse(value, out DateTime d) ? (DateTime?)d : null; }
        }
        public DateTime? Modified { get; set; }

        [XmlElement("Logs")]
        public Logs Logs { get; set; }
    }
}