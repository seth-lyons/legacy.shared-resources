using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{
    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true), XmlRoot(ElementName = "CampaignGroups", Namespace = "", IsNullable = false)]
    public partial class CampaignGroupList
    {
        [XmlElement("CampaignGroup")]
        public CampaignGroup[] CampaignGroups { get; set; }
    }

    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class CampaignGroup
    {
        [XmlAttribute("GroupId")]
        public int ID { get; set; }

        [XmlAttribute("GroupTitle")]
        public string Title { get; set; }
    }
}