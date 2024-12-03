using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{
    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true), XmlRoot(ElementName = "Fields", Namespace = "", IsNullable = false)]
    public partial class FieldList
    {
        [XmlElement("Field")]
        public Field[] Fields { get; set; }
    }

    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class Field
    {
        [XmlAttribute("FieldId")]
        public int ID { get; set; }

        [XmlAttribute("FieldTitle")]
        public string Title { get; set; }

        [XmlAttribute("FieldTypeId")]
        public string TypeID { get; set; }

        [XmlAttribute("FieldType")]
        public string Type { get; set; }

        [XmlAttribute("Value")]
        public string Value { get; set; }

        [XmlAttribute("FieldGroupId")]
        public string GroupID { get; set; }

        [XmlAttribute("Required")]
        public bool Required { get; set; }

        [XmlAttribute("ToolTip")]
        public string ToolTip { get; set; }

        [XmlAttribute("VisibilityTypeId")]
        public string VisibilityTypeID { get; set; }

        [XmlArray("FieldItems"), XmlArrayItem("FieldItem", IsNullable = true)]
        public FieldOption[] SelectOptions { get; set; }
    }

    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class FieldOption
    {
        [XmlAttribute("FieldItemId")]
        public string ID { get; set; }

        [XmlAttribute("Text")]
        public string Text { get; set; }
    }
}
