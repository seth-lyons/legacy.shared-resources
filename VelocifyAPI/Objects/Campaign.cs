using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{
    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true), XmlRoot(ElementName = "Campaigns", Namespace = "", IsNullable = false)]
    public partial class CampaignList
    {
        [XmlElement("Campaign")]
        public Campaign[] Campaigns { get; set; }
    }

    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class Campaign
    {
        [XmlAttribute("CampaignId")]
        public int ID { get; set; }

        [XmlAttribute("CampaignTitle")]
        public string Title { get; set; }

        [XmlAttribute("CampaignTypeId")]
        public string TypeID { get; set; }

        [XmlAttribute]
        public string AltTitle { get; set; }

        [XmlAttribute]
        public bool Active { get; set; }

        [XmlAttribute]
        public string CostPerLead { get; set; }

        [XmlAttribute]
        public string Note { get; set; }

        [XmlAttribute]
        public string ResponseCode { get; set; }

        [XmlAttribute("CampaignGroupId")]
        public string GroupID { get; set; }

        [XmlAttribute]
        public string CampaignGroupTitle { get; set; }

        [XmlAttribute("ProviderId")]
        public string ProviderID { get; set; }
    }
}