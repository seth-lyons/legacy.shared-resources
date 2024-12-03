using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{
    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true), XmlRoot(ElementName = "Agents", Namespace = "", IsNullable = false)]
    public partial class AgentList
    {
        [XmlAttribute("ClientId")]
        public string ClientId { get; set; }

        [XmlElement("Agent")]
        public Agent[] Agents { get; set; }
    }

    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class Agent
    {
        [XmlAttribute("AgentId")]
        public int ID { get; set; }

        [XmlAttribute("AgentName")]
        public string Name { get; set; }

        [XmlAttribute("AgentEmail")]
        public string Email { get; set; }

        [XmlAttribute("AgentStatusId")]
        public int StatusID { get; set; }

        [XmlAttribute("Position")]
        public string Position { get; set; }

        [XmlAttribute("EmailMobile")]
        public string EmailMobile { get; set; }

        [XmlAttribute("PhoneFax")]
        public string Phone_Fax { get; set; }

        [XmlAttribute("PhoneMobile")]
        public string Phone_Mobile { get; set; }

        [XmlAttribute("PhoneOther")]
        public string Phone_Other { get; set; }

        [XmlAttribute("PhoneWork")]
        public string Phone_Work { get; set; }

        [XmlAttribute("Phone_Dialer")]
        public string PhoneDialer { get; set; }

        [XmlAttribute("Note")]
        public string Note { get; set; }

        [XmlAttribute("GroupId")]
        public string GroupID { get; set; }

        [XmlAttribute("GroupName")]
        public string GroupName { get; set; }

        [XmlElement("AgentCustomFields")]
        public CustomFields CustomFields { get; set; }

        [XmlAttribute("Custom1")]
        public string Custom1 { get => CustomFields?.Custom1; set { if (CustomFields == null) CustomFields = new CustomFields(); CustomFields.Custom1 = value; } }

        [XmlAttribute("Custom2")]
        public string Custom2 { get => CustomFields?.Custom2; set { if (CustomFields == null) CustomFields = new CustomFields(); CustomFields.Custom2 = value; } }

        [XmlAttribute("Custom3")]
        public string Custom3 { get => CustomFields?.Custom3; set { if (CustomFields == null) CustomFields = new CustomFields(); CustomFields.Custom3 = value; } }

        [XmlAttribute("Custom4")]
        public string Custom4 { get => CustomFields?.Custom4; set { if (CustomFields == null) CustomFields = new CustomFields(); CustomFields.Custom4 = value; } }
    }


    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class CustomFields
    {
        [XmlAttribute("custom1")]
        public string Custom1 { get; set; }

        [XmlAttribute("custom2")]
        public string Custom2 { get; set; }

        [XmlAttribute("custom3")]
        public string Custom3 { get; set; }

        [XmlAttribute("custom4")]
        public string Custom4 { get; set; }
    }
}