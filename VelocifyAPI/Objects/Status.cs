using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{
    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true), XmlRoot(ElementName = "Statuses", Namespace = "", IsNullable = false)]
    public partial class StatusList
    {
        [XmlElement("Status")]
        public Status[] Statuses { get; set; }
    }

    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class Status
    {
        [XmlAttribute("StatusId")]
        public int ID { get; set; }

        [XmlAttribute("StatusTitle")]
        public string Title { get; set; }
    }
}