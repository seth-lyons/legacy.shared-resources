using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{
    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true), XmlRoot(ElementName = "FieldGroups", Namespace = "", IsNullable = false)]
    public partial class FieldGroupList
    {
        [XmlElement("FieldGroup")]
        public FieldGroup[] FieldGroups { get; set; }
    }

    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class FieldGroup
    {
        [XmlAttribute("FieldGroupId")]
        public int ID { get; set; }

        [XmlAttribute("FieldGroupTitle")]
        public string Title { get; set; }
    }
}