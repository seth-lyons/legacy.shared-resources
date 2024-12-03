using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{
    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true), XmlRoot(ElementName = "Groups", Namespace = "", IsNullable = false)]
    public partial class GroupList
    {
        [XmlElement("Group")]
        public Group[] Groups { get; set; }
    }

    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class Group
    {
        [XmlAttribute("GroupId")]
        public int ID { get; set; }

        [XmlAttribute("GroupTitle")]
        public string Title { get; set; }

        [XmlAttribute("Active")]
        public bool Active { get; set; }
    }
}